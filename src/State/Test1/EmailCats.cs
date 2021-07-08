using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace State.Test1
{
	class Category
	{
		public int ID { get; set; }
		public string Name { get; set; }
		public bool Personal { get; set; }
	}

	class EmailSubsctiprion
	{
		public int PersonId { get; set; }
		public int FoundationId { get; set; }
		public Dictionary<Category, SubsctiprionItem> Subsctiprion { get; set; }
	}

	public abstract class SubsctiprionItem
	{
	}

	public class PersonalSubsctiprionItem: SubsctiprionItem
	{
		public Recipient SendTo { get; set; }
		public List<Recipient> CopyTo { get; set; }
	}

	public class Recipient
	{
		public static Recipient Me = new Recipient(-1, 0);
		public static Recipient Pca = new Recipient(-2, 0);

		public int UserId { get; }
		public int MailboxId { get; }

		private Recipient(int userId, int mailboxId)
		{
			UserId = userId;
			MailboxId = mailboxId;
		}

		public static Recipient CreateFromUser(int userId) => new Recipient(userId, 0);
		public static Recipient CreateFromMailbox(int mailboxId) => new Recipient(0, mailboxId);
	}

	public class PublicSubsctiprionItem: SubsctiprionItem
	{
		public CopyToField Subscribed { get; set; }
	}

	public enum CopyToField
	{
		OptOut = 0,
		SentTo = 1,
		CopyTo = 2,
	}
}
