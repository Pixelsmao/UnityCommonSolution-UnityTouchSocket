using TouchSocket.Core;
using UnityEngine;

namespace Assets.Scenes._2D.Script
{
    /// <summary>
    /// 玩家信息
    /// </summary>
    public class GamePlayer : MonoBehaviour
    {
        public GamePlayer()
        {

            //对于单个的基础服务，将它注入到容器中
            ContainerService.container.RegisterSingleton<GamePlayer>(this);
        }

        /// <summary>
        /// 玩家ID
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// 玩家主角
        /// </summary>
        public NPCUnit GamePlayerUnit { get; set; }

    }
}
