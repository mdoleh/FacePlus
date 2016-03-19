using UnityEngine;
using System.Collections;
using Mixamo;

[ExecuteInEditMode]
public class FacePlusInGameLogin : MonoBehaviour {
	private string passPhrase = "Ips@F!ct0M3enyM!!e";
	private string USER_KEY = "FacePlus.InGame.Email";
	private string PASS_KEY = "FacePlus.InGame.Password";
	void Update() {
		if (true) {
			PlayerPrefs.DeleteKey (USER_KEY);
			PlayerPrefs.DeleteKey (PASS_KEY);
			PlayerPrefs.Save ();
		}

		if (   !Authentication.CanUseFacePlus
		    && !Authentication.IsLoggingIn
		    && PlayerPrefs.HasKey (USER_KEY)
		    && PlayerPrefs.HasKey (PASS_KEY)) {

			user = PlayerPrefs.GetString (USER_KEY);
			pass = StringCipher.Decrypt(PlayerPrefs.GetString (PASS_KEY), passPhrase);
			DoLogin ();
		}

		if (shouldStartTracking) {
			PlayerPrefs.SetString (USER_KEY, user);
			PlayerPrefs.SetString (PASS_KEY, StringCipher.Encrypt (pass, passPhrase));
			PlayerPrefs.Save ();
			Logger.Log ("InGame start live tracking!");
			FaceCapture.Instance.StartLiveTracking();
			shouldStartTracking = false;
		}
	}

	private float windowWidth = 300f;
	private float windowHeight = 100f;
	private bool shouldClearLogin = false;

	private Rect windowRect;
	void OnGUI () {
		//Error handling
		if(FacePlus.HasError && FacePlus.GetError() != ""){
			faceplus_error_message = FacePlus.GetError ();
			GUILayout.Window (0, windowRect, ErrorWindow, "Mixamo Face Plus");
		}
		if (Authentication.CanUseFacePlus 
		    && FacePlus.IsInitComplete) return;

		windowRect = new Rect(Screen.width/2 - windowWidth/2, 
                              3*Screen.height/4 - windowHeight/2, 
                              windowWidth, 
                              windowHeight);

		GUILayout.Window (0, windowRect, LoginWindow, "Mixamo Face Plus");
	}

	private string user = "Email Address";
	private string pass = "Password";
	private string login_error_message = "";
	private string faceplus_error_message = "";
	private string _login_str = null;
	private bool shouldStartTracking = false;
	void LoginWindow(int windowId) {
		GUILayout.BeginVertical ();
		GUILayout.FlexibleSpace ();
		if (!string.IsNullOrEmpty(login_error_message)) 
		{
			GUILayout.Label (string.IsNullOrEmpty(_login_str) ? login_error_message : _login_str, GUILayout.ExpandHeight(true));
		}
		else if(!string.IsNullOrEmpty(_login_str))
		{
			GUILayout.Label (_login_str, GUILayout.ExpandHeight (true));
		}
		user = GUILayout.TextField (user);
		pass = GUILayout.PasswordField(pass, '*');

		string buttonText = Authentication.IsLoggingIn ?
			"Logging In..." :
			"Log in with Mixamo Account";

		if (Authentication.CanUseFacePlus && !FacePlus.IsInitComplete) {
			buttonText = "Initializing Face Plus...";
		}

		GUI.enabled = !Authentication.IsLoggingIn 
			&& !FacePlus.IsInitComplete
			&& !Authentication.CanUseFacePlus;
		if(GUILayout.Button (buttonText)) {
			DoLogin ();
		}
		GUILayout.EndVertical();
	}

	void DoLogin() {
		FacePlus.StartUp ();
		Authentication.Login (user, pass, (msg) => {
			Debug.Log ("Logged in successfully");
			shouldStartTracking = true;
			_login_str = msg;
		}, (error,message) => {
			shouldClearLogin = true;
			login_error_message = error;
			_login_str = message;
		}, "Demo");
	}

	void ErrorWindow(int windowId){
		Color previousContent = GUI.contentColor;
		GUI.contentColor = Color.red;
		GUILayout.Label ("Faceplus Error: " + faceplus_error_message);
		GUI.contentColor = previousContent;
	}
}
