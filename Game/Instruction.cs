using System.Collections.Generic;
using Engine;
using Game;
namespace Mlfk
{
	public class Instruction
	{
		public string Name;

		public string About;

		public bool Condition;

		public bool Survival;

		public List<string> Types = new List<string>();

		public Dictionary<string, string> Demos = new Dictionary<string, string>();

		public Dictionary<string, string> Details = new Dictionary<string, string>();

		public Dictionary<string, string[]> Paras = new Dictionary<string, string[]>();

		public Dictionary<string, string> Definitions = new Dictionary<string, string>();

		public Dictionary<string, Point2> Ranges = new Dictionary<string, Point2>();

		public Dictionary<string, string> Options = new Dictionary<string, string>();
	}
}
