#r "test.dll"

//namespace Test
//{
//	public class Testicles
//	{
//		public uint TestVariable = 0u;
//	}
//}

Test.Testicles test = new();
test.TestVariable = 99u;
ScriptMessage(test.TestVariable.ToString()); // will say 99