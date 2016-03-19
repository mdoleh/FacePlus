using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Threading;
using System.IO;
using System.Net;
using System.Text;
using System;

namespace Mixamo {

	enum LoginCode {
		InternalError = -1,
		Success = 0,
		Forbidden = 401,
		ServerError = 500
	}

	public class Authentication {

		public class UserRecord {
			public string Email;
		}

		public static UserRecord User;

		public static bool IsAuthenticated {
			get {
				return (User != null);
			}
		}

		public static bool CanUseFacePlus {
			get {
				return IsAuthenticated;
			}
		}

		public static bool IsLoggingIn = false;

		public static void Logout() {
			User = null;
			FacePlus.Logout ();
		}
		public static void Login(string user, string password, Action<string> onSuccess, Action<string,string> onFailure) 
		{
			Login(user, password, onSuccess, onFailure, "unity");
		}
		public static void Login(string user, string password, Action<string> onSuccess, Action<string,string> onFailure, string client ) {

			if(IsLoggingIn){
				return;
			}

			IsLoggingIn = true;

			Thread loginThread = new Thread(() =>{
				int result;
				result = FacePlus.Login (user, password, client);
				string login_message = FacePlus.LoginString();
				if(!Enum.IsDefined(typeof(LoginCode), result)) {
					onFailure("Login Failed (Server Code: "+result+").",login_message);
				} else {
					switch((LoginCode)result) {
						case LoginCode.Success:
							User = new UserRecord() {
								Email = user
							};
							onSuccess(login_message);
							break;

						case LoginCode.Forbidden:
							onFailure("Incorrect username or password.",login_message);
							break;

						case LoginCode.InternalError:
							onFailure("Login Failed, Internal Error (" + result +").",login_message);
							break;

						case LoginCode.ServerError:
							onFailure("Server Error (500). Try again later.",login_message);
							break;

						default:
							onFailure("Login Failed (Server Code: "+result+").",login_message);
							break;
					}
				}

				IsLoggingIn = false;
			});
			loginThread.Start ();
			while(!loginThread.IsAlive) {}
		}
	}
}
