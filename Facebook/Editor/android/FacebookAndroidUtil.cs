using Facebook;
using UnityEngine;
using System.Diagnostics;
using System.Text;
using System.Collections;

namespace UnityEditor.FacebookEditor
{
	public class FacebookAndroidUtil
	{
		public const string ERROR_NO_SDK = "no_android_sdk";
		public const string ERROR_NO_KEYSTORE = "no_android_keystore";
		public const string ERROR_NO_KEYTOOL = "no_java_keytool";
		public const string ERROR_NO_OPENSSL = "no_openssl";
		public const string ERROR_KEYTOOL_ERROR = "java_keytool_error";
		
		private static string playerSettingsKeyHash;
		//private static string debugKeyHash;
		private static string setupError;
		
		// Call to reset the playerSettingsKeyhash as it may change.
		public static void refresh()
		{
			playerSettingsKeyHash = null;
		}
		
		public static bool IsSetupProperly()
		{
			return DebugKeyHash != null;
		}
		
		public static string DebugKeyHash
		{
			get
			{
				if ( playerSettingsKeyHash == null )
				{
					if ( !HasAndroidSDK() )
					{
						setupError = ERROR_NO_SDK;
						return null;
					}
					if ( !HasAndroidKeystoreFile() )
					{
						setupError = ERROR_NO_KEYSTORE;
						return null;
					}
					if ( !DoesCommandExist( "echo \"xxx\" | openssl base64" ) )
					{
						setupError = ERROR_NO_OPENSSL;
						return null;
					}
					if ( !DoesCommandExist( "keytool" ) )
					{
						setupError = ERROR_NO_KEYTOOL;
						return null;
					}
					
					// Using the user defined keystore values instead of the debug one.
					playerSettingsKeyHash = GetKeyHash( PlayerSettings.Android.keyaliasName, PlayerSettings.Android.keystoreName, PlayerSettings.Android.keyaliasPass, PlayerSettings.Android.keystorePass );
					//debugKeyHash = GetKeyHash("androiddebugkey", DebugKeyStorePath, "android");
				}
				//return debugKeyHash;
				return playerSettingsKeyHash;
			}
		}
		
		/*
        private static string DebugKeyStorePath
        {
            get
            {
                return (Application.platform == RuntimePlatform.WindowsEditor) ? 
                    System.Environment.GetEnvironmentVariable("HOMEPATH") + @"\.android\debug.keystore" : 
                    System.Environment.GetFolderPath(System.Environment.SpecialFolder.Personal) + @"/.android/debug.keystore";
            }
        }*/
		
		private static string GetKeyHash( string alias, string keyStore, string keyPass, string storePass )
		{
			var proc = new Process();
			var arguments = "";	// Split this into two seperate calls as the other call was not running correctly.
			if ( Application.platform == RuntimePlatform.WindowsEditor )
			{
				arguments = @"""keytool -storepass {0} -keypass {1} -exportcert -alias {2} -keystore ""{3}"" | openssl sha1 -binary | openssl base64""";
				proc.StartInfo.FileName = "cmd";
				arguments = @"/C " + arguments;
			}
			else
			{
				arguments = @"""keytool -storepass {0} -keypass {1} -exportcert -alias {2} -keystore {3} | openssl sha1 -binary | openssl base64""";
				proc.StartInfo.FileName = "bash";
				arguments = @"-c " + arguments;
			}
			proc.StartInfo.Arguments = string.Format( arguments, storePass, keyPass, alias, keyStore );
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.CreateNoWindow = true;
			proc.StartInfo.RedirectStandardOutput = true;
			proc.Start();
			var keyHash = new StringBuilder();
			while ( !proc.HasExited )
			{
				keyHash.Append( proc.StandardOutput.ReadToEnd() );
			}
			
			switch ( proc.ExitCode )
			{
			case 255: setupError = ERROR_KEYTOOL_ERROR;
				return null;
			}
			return keyHash.ToString().TrimEnd( '\n' );
		}
		
		public static string SetupError
		{
			get
			{
				return setupError;
			}
		}
		
		public static bool HasAndroidSDK()
		{
			return EditorPrefs.HasKey( "AndroidSdkRoot" ) && System.IO.Directory.Exists( EditorPrefs.GetString( "AndroidSdkRoot" ) );
		}
		
		public static bool HasAndroidKeystoreFile()
		{
			//return System.IO.File.Exists(DebugKeyStorePath);
			// keystoreName returns the user specified keystore path as well as the filename.
			return System.IO.File.Exists( PlayerSettings.Android.keystoreName );
		}
		
		
		private static bool DoesCommandExist( string command )
		{
			var proc = new Process();
			if ( Application.platform == RuntimePlatform.WindowsEditor )
			{
				proc.StartInfo.FileName = "cmd";
				proc.StartInfo.Arguments = @"/C" + command;
			}
			else
			{
				proc.StartInfo.FileName = "bash";
				proc.StartInfo.Arguments = @"-c " + command;
			}
			proc.StartInfo.UseShellExecute = false;
			proc.StartInfo.CreateNoWindow = true;
			proc.Start();
			proc.WaitForExit();
			if ( Application.platform == RuntimePlatform.WindowsEditor )
			{
				return proc.ExitCode == 0;
			}
			else
			{
				return proc.ExitCode != 127;
			}
		}
	}
}
