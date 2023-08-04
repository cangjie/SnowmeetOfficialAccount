using System;
using Microsoft.AspNetCore.Mvc;
using ThoughtWorks.QRCode.Codec;
using System.Drawing;
using System.IO;

namespace LuqinOfficialAccount.Controllers
{
    [Route("api/[controller]/[Action]")]
    [ApiController]
    public class QRCodeTestController : ControllerBase
    {
        
        public QRCodeTestController()
        {
        }

        [HttpGet]
        public ActionResult<string> GetQrCodeUrl(string qrCodeString)
        {
            QRCodeEncoder enc = new QRCodeEncoder();
            Bitmap bmp = enc.Encode(qrCodeString);

            return "";
        }


    }
}
