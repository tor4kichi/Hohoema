using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models.Live
{
	// 1:市場登録　2:コミュニティ参加　3:延長　4,5:未確認　6,7:地震速報　8:現在の放送ランキングの順位
	public enum NicoLiveInfoType
	{
		Ichiba = 1,
		JoinCommunity = 2,
		IncrementLiveTime = 3,
		Earthquake = 6,
		Earthquake2 = 7,
		LiveRanking = 8
	}
}
