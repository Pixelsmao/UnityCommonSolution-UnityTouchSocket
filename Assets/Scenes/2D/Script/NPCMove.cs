using Assets.Scenes._2D.Script;
using System.Threading.Tasks;
using TouchSocket.Core;
using UnityEngine;
using UnityRpcProxy_Json_HttpDmtp_2D;

/// <summary>
/// 移动控制
/// </summary>
public class NPCMove : MonoBehaviour
{
    public float speed = 5.0f;
    private Vector3 moveDirection = Vector3.zero;

    public NPCMove()
    {


        //对于单个的基础服务，将它注入到容器中
        ContainerService.container.RegisterSingleton<NPCMove>(this);

    }
    private Touch_2DWebSocket_Client Client;
    private GamePlayer gamePlayer;
    private void Awake()
    {
        this.Client = ContainerService.container.Resolve<Touch_2DWebSocket_Client>();
        this.gamePlayer = ContainerService.container.Resolve<GamePlayer>();
    }
    // Start is called before the first frame update
    private void Start()
    {

    }

    public float syncInterval = 1 / 10; // 同步间隔时间（秒）
    private float timer = 0f;


    private async void Update()
    {
        var playerUnit = this.gamePlayer.GamePlayerUnit;
        if (playerUnit != null)
        {
            // 捕获输入
            var moveHorizontal = Input.GetAxis("Horizontal");
            var moveVertical = Input.GetAxis("Vertical");

            // 计算移动方向
            this.moveDirection = new Vector3(moveHorizontal, moveVertical, 0.0f);

            this.moveDirection *= this.speed;
            this.moveDirection += this.transform.position;
            this.transform.position = this.moveDirection;
            //本机移动不通过网络确认
            this.gamePlayer.GamePlayerUnit.PlayGameUpdatePosition(this.moveDirection);

            this.timer += Time.deltaTime;
            // 达到同步间隔时间时执行同步
            if (this.timer >= this.syncInterval && (moveHorizontal != 0 || moveVertical != 0))
            {
                this.timer = 0f;
                // 发送移动请求到服务器
                await this.SendMoveRequest(this.moveDirection);
            }

        }

    }
    private async Task SendMoveRequest(Vector3 direction)
    {
        await this.Client.JsonRpc_UnitMovementAsync(new System.Numerics.Vector3(direction.x, direction.y, direction.z));
    }
}
