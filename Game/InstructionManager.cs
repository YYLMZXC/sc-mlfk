using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Engine;

namespace Game
{
	public class InstructionManager
	{
		public static Dictionary<string, Instruction> FunInstructions = new Dictionary<string, Instruction>();

		public static Dictionary<string, Instruction> ConInstructions = new Dictionary<string, Instruction>();

		public static List<HistoryEditItem> HistoryEditInstructions = new List<HistoryEditItem>();

		public static List<HistoryEditItem> CollectionInstructions = new List<HistoryEditItem>();

		public static void Initialize()
		{
			foreach (ContentInfo item in ContentManager.List())
			{
				if (!item.Filename.StartsWith("InstructionsDoc"))
				{
					continue;
				}
				string text = string.Empty;
				try
				{
					text = ContentManager.Get<string>(item.Filename.Split('.')[0]);
				}
				catch
				{
				}
				if (!string.IsNullOrEmpty(text))
				{
					string[] array = text.Split('#');
					if (array != null && array.Length > 8)
					{
						SetInstructions(array[2], array[4], false);
						SetInstructions(array[6], array[8], true);
					}
				}
			}
			LoadHistoryItems();
			SetMoreOptions();
		}

		public static Instruction GetInstruction(string name, bool iscon)
		{
			Instruction value2;
			if (!iscon)
			{
				Instruction value;
				if (FunInstructions.TryGetValue(name, out value))
				{
					return value;
				}
			}
			else if (ConInstructions.TryGetValue("if:" + name, out value2))
			{
				return value2;
			}
			Log.Error("InstructionManager:找不到" + name + "指令的定义");
			return null;
		}

		public static string GetDisplayName(string name)
		{
			if (!name.Contains(":"))
			{
				name = "cmd:" + name;
			}
			return name.Split(':')[1];
		}

		public static string GetCommandType(string name, bool iscon, string type)
		{
			Instruction instruction = GetInstruction(name, iscon);
			if (instruction != null && instruction.Types.Contains(type))
			{
				return type;
			}
			return "default";
		}

		public static string GetInstructionDemo(string name, string type, bool iscon)
		{
			Instruction instruction = GetInstruction(name, iscon);
			string value;
			if (instruction != null && instruction.Demos.TryGetValue(type, out value))
			{
				return value;
			}
			return string.Empty;
		}

		public static string GetInstructionDetail(string name, string type, bool iscon)
		{
			Instruction instruction = GetInstruction(name, iscon);
			string value;
			if (instruction != null && instruction.Details.TryGetValue(type, out value))
			{
				Regex regex = new Regex("\\((.+)\\)");
				string value2 = regex.Match(value).Value;
				if (!string.IsNullOrEmpty(value2))
				{
					return value.Replace(value2, "");
				}
				return value;
			}
			return "暂无";
		}

		public static string[] GetInstructionOption(string name, string type, string para, bool iscon)
		{
			Instruction instruction = GetInstruction(name, iscon);
			string value;
			if (instruction != null && instruction.Options.TryGetValue(type + "$" + para, out value))
			{
				return value.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			}
			return null;
		}

		public static string GetParameterName(string name, string type, string para, bool iscon)
		{
			Instruction instruction = GetInstruction(name, iscon);
			if (instruction != null)
			{
				foreach (KeyValuePair<string, string> definition in instruction.Definitions)
				{
					if (type + "$" + para == definition.Key)
					{
						return definition.Value;
					}
				}
				if (para.StartsWith("text"))
				{
					return "文本信息" + para.Replace("text", "");
				}
				if (para.StartsWith("color"))
				{
					return "颜色" + para.Replace("color", "");
				}
				if (para.StartsWith("pos"))
				{
					return "坐标" + para.Replace("pos", "");
				}
				if (para.StartsWith("eyes"))
				{
					return "视角" + para.Replace("eyes", "");
				}
				if (para.StartsWith("id"))
				{
					return "方块值" + para.Replace("id", "");
				}
				if (para.StartsWith("fid"))
				{
					return "家具序号" + para.Replace("fid", "");
				}
				if (para.StartsWith("cid"))
				{
					return "衣物序号" + para.Replace("cid", "");
				}
				if (para.StartsWith("obj"))
				{
					return "生物对象" + para.Replace("obj", "");
				}
				if (para.StartsWith("con"))
				{
					return "开闭" + para.Replace("con", "");
				}
				return "请输入值";
			}
			return "未知属性";
		}

		public static bool IsFixedParameter(string para)
		{
			int result;
			switch (para)
			{
			default:
				result = ((para == "cd") ? 1 : 0);
				break;
			case "cmd":
			case "if":
			case "type":
				result = 1;
				break;
			}
			return (byte)result != 0;
		}

		public static bool IsInvalidOption(string value, string name, string type, string para, bool iscon)
		{
			string[] instructionOption = GetInstructionOption(name, type, para, iscon);
			if (instructionOption != null)
			{
				string[] array = instructionOption;
				foreach (string text in array)
				{
					if (text.Contains(":"))
					{
						if (value == text.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries)[0].Replace(" ", ""))
						{
							return true;
						}
					}
					else if (value == text)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static Dictionary<string, Instruction> GetSurvivalList(Dictionary<string, Instruction> instructions)
		{
			Dictionary<string, Instruction> dictionary = new Dictionary<string, Instruction>();
			foreach (string key in instructions.Keys)
			{
				if (instructions[key].Survival)
				{
					dictionary.Add(key, instructions[key]);
				}
			}
			return dictionary;
		}

		public static void SetInstructions(string classify1, string classify2, bool iscon)
		{
			Dictionary<string, Instruction> dictionary = (iscon ? ConInstructions : FunInstructions);
			string text = classify1.Replace("\n", "#");
			string[] array = text.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text2 in array2)
			{
				if (text2.StartsWith("//"))
				{
					continue;
				}
				string[] array3 = text2.Split(new char[1] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				if (array3 == null || array3.Length != 2)
				{
					continue;
				}
				Instruction value;
				if (!dictionary.TryGetValue(array3[0], out value))
				{
					value = new Instruction();
					value.Name = array3[0];
					value.About = array3[1].Replace("*", "");
					value.Survival = !array3[1].Contains("*");
					value.Condition = iscon;
					if (!value.Condition)
					{
						FunInstructions[value.Name] = value;
					}
					else
					{
						ConInstructions[value.Name] = value;
					}
				}
				else
				{
					value.Name = array3[0];
					value.About = array3[1].Replace("*", "");
					value.Survival = !array3[1].Contains("*");
				}
			}
			string text3 = classify2.Replace("\n", "#");
			string[] array4 = text3.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array5 = array4;
			foreach (string text4 in array5)
			{
				if (text4.StartsWith("//"))
				{
					continue;
				}
				string[] array6 = text4.Split(new char[1] { '=' }, StringSplitOptions.RemoveEmptyEntries);
				if (array6 == null || array6.Length < 2)
				{
					continue;
				}
				string[] array7 = array6[0].Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				Instruction value2;
				if (!dictionary.TryGetValue(array7[0], out value2))
				{
					value2 = new Instruction();
					value2.Name = array7[0];
					value2.About = "暂无相关说明";
					value2.Condition = iscon;
					value2.Survival = true;
					if (!value2.Condition)
					{
						FunInstructions[value2.Name] = value2;
					}
					else
					{
						ConInstructions[value2.Name] = value2;
					}
				}
				string text5 = array7[1].Split(':')[1];
				value2.Types.Add(text5);
				value2.Demos.Add(text5, array6[0]);
				value2.Details.Add(text5, array6[1]);
				string[] array8 = new string[array7.Length - 2];
				for (int k = 0; k < array7.Length - 2; k++)
				{
					array8[k] = array7[k + 2].Split(':')[0];
				}
				value2.Paras.Add(text5, array8);
				Regex regex = new Regex("\\((.+)\\)");
				string value3 = regex.Match(array6[1]).Value;
				if (!string.IsNullOrEmpty(value3))
				{
					value3 = value3.Substring(1, value3.Length - 2);
					string[] array9 = value3.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					string[] array10 = array9;
					foreach (string text6 in array10)
					{
						Point2 point = Point2.Zero;
						string text7 = text6;
						Regex regex2 = new Regex("\\[(.+)\\]");
						string value4 = regex2.Match(text6).Value;
						if (!string.IsNullOrEmpty(value4))
						{
							text7 = text7.Replace(value4, "");
							value4 = value4.Substring(1, value4.Length - 2);
							string[] array11 = value4.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
							if (array11.Length == 2)
							{
								point = new Point2(int.Parse(array11[0]), int.Parse(array11[1]));
							}
						}
						string[] array12 = text7.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
						if (array12.Length == 2)
						{
							string key = text5 + "$" + array12[0];
							value2.Definitions.Add(key, array12[1]);
							if (point != Point2.Zero)
							{
								value2.Ranges.Add(key, point);
							}
						}
					}
				}
				if (array6.Length > 2)
				{
					string text8 = array6[2].Replace("\r", "");
					string[] array13 = text8.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
					string[] array14 = array13;
					foreach (string text9 in array14)
					{
						string[] array15 = text9.Split(new char[1] { '@' }, StringSplitOptions.RemoveEmptyEntries);
						string key2 = text5 + "$" + array15[0];
						value2.Options.Add(key2, array15[1]);
					}
				}
			}
		}

		public static void AddHistoryItem(HistoryEditItem historyEditItem)
		{
			if (historyEditItem == null)
			{
				return;
			}
			if (HistoryEditInstructions.Count > 0)
			{
				HistoryEditItem historyEditItem2 = HistoryEditInstructions[HistoryEditInstructions.Count - 1];
				if (historyEditItem2.Line == historyEditItem.Line && historyEditItem2.Collection == historyEditItem.Collection)
				{
					HistoryEditInstructions[HistoryEditInstructions.Count - 1] = historyEditItem;
				}
				else
				{
					HistoryEditInstructions.Add(historyEditItem);
				}
			}
			else
			{
				HistoryEditInstructions.Add(historyEditItem);
			}
			if (!historyEditItem.Collection)
			{
				return;
			}
			foreach (HistoryEditItem collectionInstruction in CollectionInstructions)
			{
				if (collectionInstruction.Line == historyEditItem.Line)
				{
					CollectionInstructions.Remove(collectionInstruction);
					break;
				}
			}
			CollectionInstructions.Add(historyEditItem);
		}

		public static void RemoveHistoryItem(HistoryEditItem historyEditItem)
		{
			if (historyEditItem != null)
			{
				if (CollectionInstructions.Contains(historyEditItem))
				{
					CollectionInstructions.Remove(historyEditItem);
				}
				if (HistoryEditInstructions.Contains(historyEditItem))
				{
					HistoryEditInstructions.Remove(historyEditItem);
				}
			}
		}

		public static void LoadHistoryItems()
		{
			string path = Storage.CombinePaths(DataHandle.GetCommandPath(), "HistoryEditItems.txt");
			Stream stream = Storage.OpenFile(path, OpenFileMode.CreateOrOpen);
			StreamReader streamReader = new StreamReader(stream);
			string empty = string.Empty;
			while ((empty = streamReader.ReadLine()) != null)
			{
				string[] array = empty.Split(new char[1] { '#' }, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length >= 6)
				{
					HistoryEditItem historyEditItem = new HistoryEditItem();
					historyEditItem.About = array[0];
					historyEditItem.Line = array[1];
					historyEditItem.Position = DataHandle.GetPoint3Value(array[2]);
					historyEditItem.Pass = array[3] == "True";
					historyEditItem.Condition = array[4] == "True";
					historyEditItem.Collection = array[5] == "True";
					AddHistoryItem(historyEditItem);
				}
			}
			stream.Dispose();
		}

		public static void SaveHistoryItems()
		{
			string path = Storage.CombinePaths(DataHandle.GetCommandPath(), "HistoryEditItems.txt");
			Stream stream = Storage.OpenFile(path, OpenFileMode.Create);
			using (StreamWriter streamWriter = new StreamWriter(stream))
			{
				foreach (HistoryEditItem historyEditInstruction in HistoryEditInstructions)
				{
					string value = string.Format("{0}#{1}#{2}#{3}#{4}#{5}", historyEditInstruction.About.Replace("\r", ""), historyEditInstruction.Line, historyEditInstruction.Position.ToString(), historyEditInstruction.Pass.ToString(), historyEditInstruction.Condition.ToString(), historyEditInstruction.Collection.ToString());
					streamWriter.WriteLine(value);
				}
				streamWriter.Flush();
			}
			stream.Dispose();
		}

		public static void SetMoreOptions()
		{
			Instruction instruction = GetInstruction("texture", false);
			if (instruction != null)
			{
				string text = string.Empty;
				foreach (EntityInfo value in EntityInfoManager.EntityInfos.Values)
				{
					if (!(value.KeyName == "boat"))
					{
						text = text + value.Texture.Replace("Textures/Creatures/", "") + ":" + value.DisplayName + ",";
					}
				}
				instruction.Options.Add("pakcreature$opt", text);
			}
			Instruction instruction2 = GetInstruction("audio", false);
			if (instruction2 != null)
			{
				string text2 = string.Empty;
				foreach (ContentInfo item in ContentManager.List())
				{
					if (item.ContentPath.StartsWith("Audio/"))
					{
						text2 = text2 + item.ContentPath.Replace("Audio/", "") + ",";
					}
				}
				instruction2.Options.Add("contentpak$opt", text2);
			}
			Instruction instruction3 = GetInstruction("model", false);
			if (instruction3 == null)
			{
				return;
			}
			string text3 = string.Empty;
			foreach (ContentInfo item2 in ContentManager.List())
			{
				if (item2.ContentPath.StartsWith("Models/"))
				{
					text3 = text3 + item2.ContentPath.Replace("Models/", "") + ",";
				}
			}
			instruction3.Options.Add("pakmodel$opt", text3);
		}
	}
}
