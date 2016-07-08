using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace NicoPlayerHohoema.Models
{
	[DataContract]
	public class AccountSettings : BindableBase
	{
		public AccountSettings()
			: base()
		{
			MailOrTelephone = "";
			Password = "";
		}



		private string _MailOrTelephone;

		[DataMember]
		public string MailOrTelephone
		{
			get
			{
				return _MailOrTelephone;
			}
			set
			{
				SetProperty(ref _MailOrTelephone, value);
			}
		}


		public bool IsValidMailOreTelephone
		{
			get
			{
				return !String.IsNullOrWhiteSpace(MailOrTelephone);
			}
		}



		private string _Password;

		[DataMember]
		public string Password
		{
			get
			{
				return _Password;
			}
			set
			{
				SetProperty(ref _Password, value);
			}
		}


		public bool IsValidPassword
		{
			get
			{
				return !String.IsNullOrWhiteSpace(Password);
			}
		}
	}
}
