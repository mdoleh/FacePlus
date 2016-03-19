using UnityEngine;
using System.Collections;

namespace Mixamo {
	public interface IFacePlusVideo {
		uint DurationFrames { get; }
		uint PositionFrames { get; set; }
		float DurationSeconds { get; }
		float PositionSeconds { get; set; }
		int DisplayFrame { get; }
		//int DisplayFrameCount{ get; } //for QuickTime
		bool LoadMovie(string folder, string filename, bool val);
		Texture OutputTexture { get; }
		float FrameRate { get; }
		void Rewind();
		void Dispose();
		void Play();
		void Pause();
		void UpdateMovie(bool force);

#if UNITY_EDITOR_OSX
		bool SeekToNextFrame(); //for QuickTime
#endif
	}
}
