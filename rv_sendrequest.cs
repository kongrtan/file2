using Microsoft.AspNetCore.Mvc;
using TIBCO.Rendezvous;

namespace MyApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly NetTransport _transport;
        private readonly Queue _queue;

        public OrderController()
        {
            // RV Transport 생성 (TCP 7500 예시)
            _transport = new NetTransport("7500", null, null);
            _queue = new Queue();
        }

        [HttpPost("send")]
        public IActionResult SendOrder([FromBody] OrderRequest requestDto)
        {
            try
            {
                // 1. 임시 reply inbox 생성
                string inbox = _transport.CreateInbox();

                // 2. Listener 생성 (reply 받을 inbox 구독)
                Listener listener = new Listener(_queue, _transport, inbox, null);

                // 3. RV 요청 메시지 생성
                Message requestMsg = new Message();
                requestMsg.SendSubject = "service.order";
                requestMsg.ReplySubject = inbox;
                requestMsg.UpdateString("orderId", requestDto.OrderId);
                requestMsg.UpdateInt("amount", requestDto.Amount);

                // 4. SendRequest 호출 (동기, 타임아웃 5초)
                Message response = listener.Transport.SendRequest(requestMsg, 5000);

                // 5. 응답 처리
                string status = response.GetString("status");
                return Ok(new { orderId = requestDto.OrderId, status });
            }
            catch (RvException ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }

    public class OrderRequest
    {
        public string OrderId { get; set; }
        public int Amount { get; set; }
    }

       [HttpPost("send")]
    public IActionResult SendMessage([FromBody] MyRequest dto)
    {
        // Queue 및 Listener는 요청마다 새로 생성 가능
        Queue queue = new Queue();
        string inbox = _transport.CreateInbox();
        Message responseMsg = null;

        Listener listener = new Listener(queue, _transport, inbox, null);
        listener.OnMessage += (msg) => { responseMsg = msg; };

        Message req = new Message();
        req.SendSubject = "service.test";
        req.ReplySubject = inbox;
        req.UpdateString("data", dto.Data);

        _transport.Send(req);

        DateTime start = DateTime.Now;
        while ((DateTime.Now - start).TotalMilliseconds < 3000)
        {
            if (queue.TimedDispatch(200) && responseMsg != null)
                return Ok(new { reply = responseMsg.GetString("reply") });
        }

        return StatusCode(504, "RV response timeout");
    }
}

public record MyRequest(string Data);
}
