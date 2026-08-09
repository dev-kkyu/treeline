using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Treeline
{
    /// <summary>
    /// 장 안에서 어디까지 진행했는지. 체크포인트에서 베껴 두었다가 재시작 뒤에 되돌린다.
    /// </summary>
    /// <remarks>
    /// <b>재시작은 씬을 통째로 다시 읽는다.</b> 그래서 체크포인트만 두면 플레이어가
    /// 서는 자리만 앞당겨지고 레버와 장치는 전부 처음으로 돌아간다. 발전기 앞에서
    /// 부활했는데 차단기가 다시 내려가 있으면, 시작 지점에서 다시 하는 것보다 오히려
    /// 헷갈린다.
    /// <para>
    /// <see cref="PlayerLoadout"/> 이 소지품을 장 너머로 들고 가는 것과 같은 모양이다.
    /// 다른 점은 <b>같은 장을 다시 시작할 때만</b> 쓴다는 것이다 — 장이 바뀌면 버린다.
    /// </para>
    /// <para>
    /// IMPORTANT: 짝을 맞추는 열쇠는 <b>계층 경로</b>다. 같은 씬을 다시 읽으므로 경로는
    /// 그대로지만, <b>씬에서 오브젝트 이름을 바꾸면 그 자리의 진행이 조용히 사라진다.</b>
    /// 이름을 바꿔야 하면 그 장을 처음부터 한 번 돌려 확인한다.
    /// </para>
    /// </remarks>
    public sealed class ChapterState
    {
        private readonly Dictionary<string, bool> devices = new Dictionary<string, bool>();

        /// <summary>지금 씬의 레버와 장치 상태를 베낀다.</summary>
        public static ChapterState Capture()
        {
            ChapterState state = new ChapterState();

            foreach (Device device in Object.FindObjectsByType<Device>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                state.devices[PathOf(device.transform)] = device.IsActive;
            }

            // 레버는 베끼지 않는다. 되돌릴 때 자기 장치에서 끌어내는 편이 어긋나지 않는다.
            return state;
        }

        /// <summary>
        /// 베껴 둔 상태를 새로 읽은 씬에 되돌린다. <b>소리와 이벤트는 내지 않는다.</b>
        /// </summary>
        /// <remarks>
        /// 되돌리면서 소리를 내면 부활하자마자 차단기 소리와 문 열리는 소리가 한꺼번에
        /// 울린다. 이미 지나온 일이라 다시 들릴 이유가 없다.
        /// </remarks>
        public void Apply()
        {
            // 장치를 먼저 되돌린다. 장치가 켜 주는 레버가 있어서다 — 4장 발전기 기동
            // 레버는 차단기 2 가 켜기 전까지 꺼져 있고, 꺼진 채로 손대면 아무것도 안 된다.
            foreach (Device device in Object.FindObjectsByType<Device>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (devices.TryGetValue(PathOf(device.transform), out bool active))
                {
                    device.Restore(active);
                }
            }

            // 장치가 자리를 잡은 뒤에 레버 모습을 거기에 맞춘다.
            foreach (Lever lever in Object.FindObjectsByType<Lever>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                lever.Restore();
            }
        }

        /// <summary>이름이 겹쳐도 갈리도록 루트부터의 경로로 잡는다.</summary>
        private static string PathOf(Transform t)
        {
            StringBuilder sb = new StringBuilder(t.name);

            for (Transform p = t.parent; p != null; p = p.parent)
            {
                sb.Insert(0, '/').Insert(0, p.name);
            }

            return sb.ToString();
        }

        public override string ToString()
        {
            int on = 0;
            foreach (KeyValuePair<string, bool> kv in devices)
            {
                if (kv.Value)
                {
                    on++;
                }
            }

            return $"장치 {on}/{devices.Count} 켜짐";
        }
    }
}
