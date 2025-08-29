using Assets.Scenes._2D.Script;
using System.Threading.Tasks;
using TouchSocket.Core;
using UnityEngine;
using UnityRpcProxy_Json_HttpDmtp_2D;

/// <summary>
/// �ƶ�����
/// </summary>
public class NPCMove : MonoBehaviour
{
    public float speed = 5.0f;
    private Vector3 moveDirection = Vector3.zero;

    public NPCMove()
    {


        //���ڵ����Ļ������񣬽���ע�뵽������
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

    public float syncInterval = 1 / 10; // ͬ�����ʱ�䣨�룩
    private float timer = 0f;


    private async void Update()
    {
        var playerUnit = this.gamePlayer.GamePlayerUnit;
        if (playerUnit != null)
        {
            // ��������
            var moveHorizontal = Input.GetAxis("Horizontal");
            var moveVertical = Input.GetAxis("Vertical");

            // �����ƶ�����
            this.moveDirection = new Vector3(moveHorizontal, moveVertical, 0.0f);

            this.moveDirection *= this.speed;
            this.moveDirection += this.transform.position;
            this.transform.position = this.moveDirection;
            //�����ƶ���ͨ������ȷ��
            this.gamePlayer.GamePlayerUnit.PlayGameUpdatePosition(this.moveDirection);

            this.timer += Time.deltaTime;
            // �ﵽͬ�����ʱ��ʱִ��ͬ��
            if (this.timer >= this.syncInterval && (moveHorizontal != 0 || moveVertical != 0))
            {
                this.timer = 0f;
                // �����ƶ����󵽷�����
                await this.SendMoveRequest(this.moveDirection);
            }

        }

    }
    private async Task SendMoveRequest(Vector3 direction)
    {
        await this.Client.JsonRpc_UnitMovementAsync(new System.Numerics.Vector3(direction.x, direction.y, direction.z));
    }
}
