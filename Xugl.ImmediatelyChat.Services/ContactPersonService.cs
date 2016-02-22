using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.Services
{
    public class ContactPersonService : IContactPersonService
    {
        private readonly IRepository<ContactPerson> contactPersonRepository;
        private readonly IRepository<ContactGroupSub> contactGroupSubRepository;
        private readonly IRepository<ContactGroup> contactGroupRepository;

        public ContactPersonService(IRepository<ContactPerson> contactPersonRepository, IRepository<ContactGroupSub> contactGroupSubRepository,
            IRepository<ContactGroup> contactGroupRepository)
        {
            this.contactPersonRepository = contactPersonRepository;
            this.contactGroupSubRepository = contactGroupSubRepository;
        }

        public IList<ContactPerson> GetContactPersonIDListByGroupID(string groupID)
        {

            var contactPersons = from contactGroup in contactGroupSubRepository.Table
                                 join contactPerson in contactPersonRepository.Table on contactGroup.ContactPersonObjectID equals contactPerson.ObjectID
                                 where contactGroup.ContactGroupID == groupID
                                 select contactPerson;
            return contactPersons.ToList();
        }

        public int InsertNewPerson(ContactPerson contactPerson)
        {
            return contactPersonRepository.Insert(contactPerson);
        }

        public int InsertDefaultGroup(string ObjectID)
        {
            if(String.IsNullOrEmpty(ObjectID))
            {
                return 0;
            }

            ContactGroup contactGroup= contactGroupRepository.Table.Where(t => t.GroupName == "Group1").FirstOrDefault();
            if(contactGroup==null)
            {
                contactGroup = new ContactGroup();
                contactGroup.GroupName = "Group1";
                contactGroup.GroupObjectID = "Group1";
                contactGroupRepository.Insert(contactGroup);
            }

            ContactGroupSub contactGroupSub = new ContactGroupSub();
            contactGroupSub.ContactGroupID = contactGroup.GroupObjectID;
            contactGroupSub.ContactPersonObjectID = ObjectID;
            return contactGroupSubRepository.Insert(contactGroupSub);

        }
    }
}
