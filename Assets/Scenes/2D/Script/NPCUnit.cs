using UnityEngine;

namespace Assets.Scenes._2D.Script
{
    /// <summary>
    /// 人物单位
    /// </summary>
    public class NPCUnit : MonoBehaviour
    {
        public int ID;
        public Animator animator;

        private void Start()
        {
            this.animator = this.GetComponent<Animator>();
        }

        //public bool isPlayer;

        public Vector3 targetPosition;
        public float snapTime;
        public float snapSpeed;
        private void Update()
        {
            // 平滑过渡到目标位置
            if (this.snapTime > 0)
            {
                var t = (Time.time - this.snapTime) / this.snapSpeed; // 过渡时间
                this.LerpMovePostion(t);
            }
        }

        /// <summary>
        /// 平滑移动
        /// </summary>
        private void LerpMovePostion(float t)
        {
            var scale = this.transform.localScale;
            if (this.targetPosition.x > this.transform.position.x)
            {
                scale.x = 1;
            }
            else if (this.targetPosition.x < this.transform.position.x)
            {
                scale.x = -1;
            }
            if (this.targetPosition != this.transform.position)
            {
                this.animator.SetFloat("Speed", 1);
            }
            else
            {
                this.animator.SetFloat("Speed", 0);
            }
            this.transform.localScale = scale;
            this.transform.position = Vector3.Lerp(this.transform.position, this.targetPosition, t);
        }

        public void UpdatePosition(Vector3 newPosition)
        {
            this.snapTime = Time.time;
            this.targetPosition = newPosition;
        }
        public void PlayGameUpdatePosition(Vector3 newPosition)
        {
            this.targetPosition = newPosition;
            this.LerpMovePostion(1);
        }
    }
}
