using System.Collections.Generic;
using TouchSocket.Core;
using UnityEngine;

namespace Assets.Scenes._2D.Script
{
    /// <summary>
    /// NPC管理池
    /// </summary>
    public class NPCPool : MonoBehaviour, INPCPool
    {
        public NPCPool()
        {
            //对于单个的基础服务，将它注入到容器中
            ContainerService.container.RegisterSingleton<NPCPool>(this);

        }

        /// <summary>
        /// NPC预制体
        /// </summary>
        public NPCUnit NPCPref;

        public Dictionary<int, NPCUnit> NPS = new Dictionary<int, NPCUnit>();


        /// <summary>
        /// 创建新的人物
        /// </summary>
        public void CreateCharacter(int ID, Vector3 postion)
        {
            if (this.NPS.TryAdd(ID, null))
            {
                var newNPC = Instantiate(this.NPCPref, postion, Quaternion.identity);
                newNPC.ID = ID;
                this.NPS[ID] = newNPC;
            }
        }

        public void DestroyNPC(int ID)
        {
            if (this.TryGetValue(ID, out var npc))
            {
                Destroy(npc.gameObject);
                this.NPS.Remove(ID);
            }
        }

        public bool TryGetValue(int ID, out NPCUnit npc)
        {
            return this.NPS.TryGetValue(ID, out npc);
        }
    }
    public interface INPCPool
    {
        /// <summary>
        /// 创建新的人物
        /// </summary>
        void CreateCharacter(int ID, Vector3 postion);

        /// <summary>
        /// 获取NPC
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="npc"></param>
        /// <returns></returns>
        bool TryGetValue(int ID, out NPCUnit npc);

        /// <summary>
        /// 销毁NPC
        /// </summary>
        /// <param name="ID"></param>
        void DestroyNPC(int ID);
    }
}
