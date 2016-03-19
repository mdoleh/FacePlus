using UnityEngine;
using System.Collections;

namespace Mixamo {
	public class Logger {
		public enum LogLevel {
			None=0,
			Log=1,
			Info=2,
			Debug=3,
		};
		
		public static LogLevel Level = LogLevel.Info;
		
		public static void Log(object msg) {
			if (Level >= LogLevel.Log) {
				UnityEngine.Debug.Log (msg + "\n");
			}
		}
		public static void Info(object msg) {
			if (Level >= LogLevel.Info) 
				UnityEngine.Debug.Log (msg+ "\n");
		}
		public static void Debug(object msg) {
			if (Level >= LogLevel.Debug) 
				UnityEngine.Debug.Log (msg+ "\n");
		}
	}
}
