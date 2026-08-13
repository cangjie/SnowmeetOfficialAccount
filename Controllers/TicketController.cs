using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;


using SnowmeetOfficialAccount.Models;
namespace SnowmeetOfficialAccount.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TicketController : ControllerBase
    {
        private readonly AppDBContext _context;
        private IConfiguration _config;
        private IConfiguration _oriConfig;
        public string _appId = "";

        public TicketController(AppDBContext context, IConfiguration config)
        {
            _context = context;
            _oriConfig = config;
            _config = config.GetSection("Settings");
            _appId = _config.GetSection("AppId").Value.Trim();
        }
        [NonAction]
        public async Task<Ticket> CreateTicketByUnipayOrder(Models.Order order)
        {
            string memo = "unipay_" + order.id.ToString();
            Ticket? oriT = await _context.ticket.Where(t => t.create_memo == memo).AsNoTracking().FirstOrDefaultAsync();
            if (oriT != null)
            {
                return null;
            }
            int templateId = 12;
            Ticket ticket = await CreateTicket(templateId, order.member_id, null, "unipay_" + order.id.ToString(), "养护", null, true);
            return ticket;
        } 
        [NonAction]
        public async Task<Ticket> CreateTicket(int templateId, int? memberId, int? staffId,
            string? createMemo = null, string? bizType = null, int? bizId = null, 
            bool active = false, DateTime? startDate = null, DateTime? expireDate = null)
        {
            TicketTemplate template = await _context.ticketTemplate
                .Where(t => t.id == templateId).AsNoTracking().FirstOrDefaultAsync();
            if (memberId == null && staffId == null)
            {
                return null;
            }
            string code = await GetNewTicketCode();
            Ticket ticket = new Ticket()
            {
                code = code,
                template_id = templateId,
                name = template.name,
                member_id = memberId,
                create_memo = createMemo,
                memo = template.memo.Trim(),
                create_date = DateTime.Now,
                valid = 1,
                is_active = active?1:0,
                start_date = startDate,
                expire_date = expireDate,
                biz_id = bizId,
                biz_type = bizType
            };
            await _context.ticket.AddAsync(ticket);
            await _context.SaveChangesAsync();
            return await GetWholeTicket(code);
        } 
        [NonAction]
        public async Task<Ticket> GetWholeTicket(string code)
        {
            Ticket ticket = await _context.ticket.Where(t => t.code == code)
                .Include(t => t.template).ThenInclude(t => t.productTicketTemplates.Where(p => p.valid))
                .AsNoTracking().FirstOrDefaultAsync();
            return ticket;
        }
        [NonAction]
        public async Task<string> GetNewTicketCode()
        {
            string code = "";
            for (int i = 0; i < 100; i++)
            {
                code = Util.GetRandomCode(9);
                Ticket ticket = await _context.ticket.FindAsync(code);
                if (ticket == null)
                {
                    break;
                }
            }
            return code;
        }     
    }
}
