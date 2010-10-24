
using System;
using System.Collections.Generic;
namespace Banshee.Ampache
{
	public class Tag : IEntity
	{
		#region IEntity implementation
		public int Id { get; set; }
		#endregion
		public int Count { get; set; }
		public string Name { get; set; }
	}
}
