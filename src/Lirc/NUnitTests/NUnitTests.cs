using NUnit.Framework;
using Banshee.Lirc;

[TestFixture]
public class NUnitTest
{
	[Test]
	public void Test1()
	{
		ActionMapper am = new ActionMapper(new MockController());
		am.DispatchAction("play");
	}
}