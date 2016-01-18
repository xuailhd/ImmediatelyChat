using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.Model.QueryCondition;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.Services
{
    public class MsgRecordService : IMsgRecordService
    {
        private readonly IRepository<MsgRecord> msgRecordRepository;
        private readonly IRepository<ContactGroupSub> contactGroupSubRepository;
        private readonly IDbContext context;

        public MsgRecordService(IRepository<MsgRecord> _msgRecordRepository, IDbContext _context)
        {
            msgRecordRepository = _msgRecordRepository;
            context = _context;
        }

        public IList<MsgRecord> LoadMsgRecord(MsgRecordQuery query)
        {
            throw new NotImplementedException();
        }

        public int BatchSave(IList<MsgRecord> msgRecords)
        {
            //msgRecordRepository.
            return 1;
        }


        public IList<string> GenerateRecordByGroup(MsgRecordModel msgRecordModel)
        {

            IList<string> recipientObjectIDs = new List<string>();

            IList<ContactGroupSub> contactGroupSubs = contactGroupSubRepository.Table.
                Where(t => t.ContactGroupID == msgRecordModel.RecivedObjectID).ToList();

            foreach(ContactGroupSub contactGroupSub in contactGroupSubs)
            {
                recipientObjectIDs.Add(contactGroupSub.ContactPersonObjectID);
            }
            return recipientObjectIDs;
        }
    }
}
