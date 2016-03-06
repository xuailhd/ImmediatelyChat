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
        private readonly IRepository<ContactPersonList> contactPersonListRepository;

        public ContactPersonService(IRepository<ContactPerson> contactPersonRepository, IRepository<ContactGroupSub> contactGroupSubRepository,
            IRepository<ContactGroup> contactGroupRepository, IRepository<ContactPersonList> contactPersonListRepository)
        {
            this.contactPersonRepository = contactPersonRepository;
            this.contactGroupSubRepository = contactGroupSubRepository;
            this.contactGroupRepository = contactGroupRepository;
            this.contactPersonListRepository = contactPersonListRepository;
        }

        public IList<ContactPerson> GetContactPersonIDListByGroupID(string groupID)
        {

            var contactPersons = from contactGroup in contactGroupSubRepository.Table
                                 join contactPerson in contactPersonRepository.Table on contactGroup.ContactPersonObjectID equals contactPerson.ObjectID
                                 where contactGroup.ContactGroupID == groupID
                                 select contactPerson;
            return contactPersons.ToList();
        }


        public ContactPerson FindContactPerson(string objectID)
        {
            return null;
        }

        public int InsertNewPerson(ContactPerson contactPerson)
        {
            return contactPersonRepository.Insert(contactPerson);
        }

        public int UpdateContactPerson(ContactPerson contactPerson)
        {
            return 0;
        }

        public int InsertDefaultGroup(string ObjectID)
        {
            if(string.IsNullOrEmpty(ObjectID))
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
            contactGroupSub.UpdateTime = DateTime.Now;
            return contactGroupSubRepository.Insert(contactGroupSub);

        }

        public IList<ContactGroup> GetLastestContactGroup(string objectID, DateTime updateTime)
        {
            return null;
        }
    

        public IList<ContactGroupSub> GetLastestContactGroupSub(string objectID, DateTime updateTime)
        {
            if(string.IsNullOrEmpty(objectID))
            {
                return null;
            }
            var query = contactGroupSubRepository.Table.Where(t => t.ContactPersonObjectID == objectID && DateTime.Compare(t.UpdateTime, updateTime) > 0);

            return query.ToList();
        }

        public IList<ContactPersonList> GetLastestContactPersonList(string objectID, DateTime updateTime)
        {
            if (string.IsNullOrEmpty(objectID))
            {
                return null;
            }
            var query = contactPersonListRepository.Table.Where(t => t.ObjectID == objectID && DateTime.Compare(t.UpdateTime, updateTime) > 0);

            return query.ToList();
        }

        public ContactPersonList FindContactPersonList(string objectID, string destinationObjectID)
        {
            return null;
        }
        public int InsertContactPersonList(ContactPersonList contactPersonList)
        {
            return 0;
        }
        public int UpdateContactPersonList(ContactPersonList contactPersonList)
        {
            return 0;
        }

        public ContactGroup FindContactGroup(string groupID)
        {
            return null;
        }
        public int InsertNewGroup(ContactGroup contactGroup)
        {
            return 0;
        }
        public int UpdateContactGroup(ContactGroup contactGroup)
        {
            return 0;
        }

        public ContactGroupSub FindContactGroupSub(string groupID, string contactPersonObjectID)
        {
            return null;
        }
        public int InsertContactGroupSub(ContactGroupSub contactGroupSub)
        {
            return 0;
        }

        public int UpdateContactGroupSub(ContactGroupSub contactGroupSub)
        {
            return 0;
        }
    }
}
