using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace Mixamo {
	public interface IChannelMapper {
		Dictionary<string, AnimationTarget> CreateMap();
	}
}


