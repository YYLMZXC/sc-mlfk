using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Engine;
using Game;
namespace Mlfk
{
	public class DataHandle
	{
		public static Color[] ColorList = new Color[16]
		{
			new Color(255, 255, 255, 255),
			new Color(181, 255, 255, 255),
			new Color(255, 181, 255, 255),
			new Color(160, 181, 255, 255),
			new Color(255, 240, 160, 255),
			new Color(181, 255, 181, 255),
			new Color(255, 181, 160, 255),
			new Color(181, 181, 181, 255),
			new Color(112, 112, 112, 255),
			new Color(32, 112, 112, 255),
			new Color(70, 20, 70, 255),
			new Color(26, 52, 128, 255),
			new Color(87, 54, 31, 255),
			new Color(24, 116, 24, 255),
			new Color(136, 32, 32, 255),
			new Color(24, 24, 24, 255)
		};

		public static Color[] ElectronColorList = new Color[9]
		{
			new Color(0, 0, 0),
			new Color(255, 255, 255),
			new Color(0, 255, 255),
			new Color(255, 0, 0),
			new Color(0, 0, 255),
			new Color(255, 240, 0),
			new Color(0, 255, 0),
			new Color(255, 120, 0),
			new Color(255, 0, 255)
		};

		public static short[][] StairStates = new short[24][]
		{
			new short[8] { 1, 1, 1, 1, 1, 0, 1, 0 },
			new short[8] { 1, 1, 1, 1, 1, 1, 0, 0 },
			new short[8] { 1, 1, 1, 1, 0, 1, 0, 1 },
			new short[8] { 1, 1, 1, 1, 0, 0, 1, 1 },
			new short[8] { 1, 0, 1, 0, 1, 1, 1, 1 },
			new short[8] { 1, 1, 0, 0, 1, 1, 1, 1 },
			new short[8] { 0, 1, 0, 1, 1, 1, 1, 1 },
			new short[8] { 0, 0, 1, 1, 1, 1, 1, 1 },
			new short[8] { 1, 1, 1, 1, 0, 0, 1, 0 },
			new short[8] { 1, 1, 1, 1, 1, 0, 0, 0 },
			new short[8] { 1, 1, 1, 1, 0, 1, 0, 0 },
			new short[8] { 1, 1, 1, 1, 0, 0, 0, 1 },
			new short[8] { 0, 0, 1, 0, 1, 1, 1, 1 },
			new short[8] { 1, 0, 0, 0, 1, 1, 1, 1 },
			new short[8] { 0, 1, 0, 0, 1, 1, 1, 1 },
			new short[8] { 0, 0, 0, 1, 1, 1, 1, 1 },
			new short[8] { 1, 1, 1, 1, 1, 0, 1, 1 },
			new short[8] { 1, 1, 1, 1, 1, 1, 1, 0 },
			new short[8] { 1, 1, 1, 1, 1, 1, 0, 1 },
			new short[8] { 1, 1, 1, 1, 0, 1, 1, 1 },
			new short[8] { 1, 0, 1, 1, 1, 1, 1, 1 },
			new short[8] { 1, 1, 1, 0, 1, 1, 1, 1 },
			new short[8] { 1, 1, 0, 1, 1, 1, 1, 1 },
			new short[8] { 0, 1, 1, 1, 1, 1, 1, 1 }
		};

		public static short[][] SlabStates = new short[2][]
		{
			new short[8] { 1, 1, 1, 1, 0, 0, 0, 0 },
			new short[8] { 0, 0, 0, 0, 1, 1, 1, 1 }
		};

		public static int GetStairValue(int value, int l)
		{
			int num = Terrain.ExtractContents(value);
			int data = Terrain.ExtractData(value);
			int? color = StairsBlock.GetColor(data);
			bool flag = StairStates[StairsBlock.GetVariant(data)][l] == 1;
			switch (num)
			{
			case 49:
				num = 21;
				break;
			case 217:
				num = 3;
				break;
			case 48:
				num = 5;
				break;
			case 50:
				num = 26;
				break;
			case 76:
				num = 73;
				break;
			case 51:
				num = 4;
				break;
			case 69:
				num = 68;
				break;
			case 96:
				num = 67;
				break;
			default:
				return 0;
			}
			if (!flag)
			{
				return 0;
			}
			return color.HasValue ? Terrain.MakeBlockValue(num, 0, PaintedCubeBlock.SetColor(0, color.Value)) : num;
		}

		public static int GetSlabValue(int value, int l)
		{
			int num = Terrain.ExtractContents(value);
			int data = Terrain.ExtractData(value);
			int? color = SlabBlock.GetColor(data);
			bool flag = SlabStates[SlabBlock.GetIsTop(data) ? 1 : 0][l] == 1;
			switch (num)
			{
			case 55:
				num = 21;
				break;
			case 136:
				num = 3;
				break;
			case 53:
				num = 5;
				break;
			case 54:
				num = 26;
				break;
			case 75:
				num = 73;
				break;
			case 52:
				num = 4;
				break;
			case 70:
				num = 68;
				break;
			case 95:
				num = 67;
				break;
			default:
				return 0;
			}
			if (!flag)
			{
				return 0;
			}
			return color.HasValue ? Terrain.MakeBlockValue(num, 0, PaintedCubeBlock.SetColor(0, color.Value)) : num;
		}

		public static Color GetCommandColor(int color)
		{
			Color result = Color.Green;
			switch (color)
			{
			case 0:
				result = new Color(0, 255, 0);
				break;
			case 1:
				result = new Color(181, 255, 255);
				break;
			case 2:
				result = new Color(255, 181, 255);
				break;
			case 3:
				result = new Color(160, 181, 255);
				break;
			case 4:
				result = new Color(255, 240, 160);
				break;
			case 5:
				result = new Color(181, 255, 181);
				break;
			case 6:
				result = new Color(255, 181, 160);
				break;
			case 7:
				result = new Color(181, 181, 181);
				break;
			case 8:
				result = new Color(112, 112, 112);
				break;
			case 9:
				result = new Color(0, 255, 255);
				break;
			case 10:
				result = new Color(255, 0, 255);
				break;
			case 11:
				result = new Color(0, 0, 255);
				break;
			case 12:
				result = new Color(225, 112, 0);
				break;
			case 13:
				result = new Color(0, 255, 0);
				break;
			case 14:
				result = new Color(255, 0, 0);
				break;
			case 15:
				result = new Color(24, 24, 24);
				break;
			}
			return result;
		}

		public static int GetColorIndex(Color c, int type = 0)
		{
			int result = 0;
			Color[] array = ColorList;
			if (type == 1)
			{
				array = ElectronColorList;
			}
			float[] array2 = new float[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				Color color = array[i];
				int num = (c.R + color.R) / 2;
				int num2 = (c.R - color.R) * (c.R - color.R);
				int num3 = (c.G - color.G) * (c.G - color.G);
				int num4 = (c.B - color.B) * (c.B - color.B);
				array2[i] = (2 + num / 256) * num2 + 4 * num3 + (2 + (255 - num) / 256) * num4;
			}
			float num5 = float.MaxValue;
			for (int j = 0; j < array2.Length; j++)
			{
				if (array2[j] < num5)
				{
					num5 = array2[j];
					result = j;
				}
			}
			return result;
		}

		public static string NumberToSignal(int n)
		{
			if (n < 10 && n >= 0)
			{
				return n.ToString();
			}
			switch (n)
			{
			case 10:
				return "a";
			case 11:
				return "b";
			case 12:
				return "c";
			case 13:
				return "d";
			case 14:
				return "e";
			case 15:
				return "f";
			default:
				return "0";
			}
		}

		public static bool GetBoolValue(string str)
		{
			return str.ToLower() == "true";
		}

		public static Vector2 GetVector2Value(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			return new Vector2(int.Parse(array[0]), int.Parse(array[1]));
		}

		public static Vector3 GetVector3Value(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			return new Vector3(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
		}

		public static Vector4 GetVector4Value(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			return new Vector4(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]), int.Parse(array[3]));
		}

		public static Point2 GetPoint2Value(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			return new Point2(int.Parse(array[0]), int.Parse(array[1]));
		}

		public static Point3 GetPoint3Value(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			return new Point3(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
		}

		public static Color GetColorValue(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			if (array.Length == 3)
			{
				return new Color(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]));
			}
			if (array.Length == 4)
			{
				return new Color(int.Parse(array[0]), int.Parse(array[1]), int.Parse(array[2]), int.Parse(array[3]));
			}
			return Color.White;
		}

		public static DateTime GetDateTimeValue(string str)
		{
			string[] array = str.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			int year = MathUtils.Clamp(int.Parse(array[0]), 2012, 2050);
			int month = MathUtils.Clamp(int.Parse(array[1]), 1, 12);
			int day = MathUtils.Clamp(int.Parse(array[2]), 1, 31);
			int hour = MathUtils.Clamp(int.Parse(array[3]), 0, 23);
			int minute = MathUtils.Clamp(int.Parse(array[4]), 0, 59);
			int second = MathUtils.Clamp(int.Parse(array[5]), 0, 59);
			return new DateTime(year, month, day, hour, minute, second);
		}

		public static int GetNaturalValue(string str)
		{
			int num = int.Parse(str);
			return (num >= 0) ? num : 0;
		}

		public static string CharacterEscape(string str)
		{
			return str.Replace("[n]", "\n").Replace("[e]", " ").Replace("[c]", ":")
				.Replace("[d]", "=");
		}

		public static bool IsContainsVariable(string str)
		{
			List<string> list = new List<string>();
			string[] array = str.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = array;
			foreach (string text in array2)
			{
				string[] array3 = text.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				if (array3[0].Contains("fix") || array3[0].Contains("opt"))
				{
					list.Add(array3[0] + ":" + array3[1]);
				}
			}
			if (list.Count != 0)
			{
				foreach (string item in list)
				{
					str = str.Replace(item, "");
				}
			}
			return str.Contains("X") || str.Contains("Y") || str.Contains("V") || str.Contains("W");
		}

		public static CommandData SetVariableData(CommandData commandData, int[] signals)
		{
			foreach (string key in commandData.DataText.Keys)
			{
				try
				{
					if (!IsContainsVariable(key + ":" + commandData.DataText[key]))
					{
						continue;
					}
					commandData.Data[key] = ReplaceVariable(commandData.DataText[key], signals);
					string[] array = ((string)commandData.Data[key]).Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					if (array.Length > 1)
					{
						for (int i = 0; i < array.Length; i++)
						{
							if (IsExpression(array[i]))
							{
								array[i] = ExpressionHandle(array[i]);
							}
						}
						commandData.Data[key] = string.Join(",", array);
					}
					else if (IsExpression((string)commandData.Data[key]))
					{
						commandData.Data[key] = ExpressionHandle((string)commandData.Data[key]);
					}
				}
				catch
				{
				}
			}
			commandData.FastSetValue();
			return commandData;
		}

		public static string ExpressionHandle(string str)
		{
			int num = 0;
			string text = "";
			TStack tStack = new TStack();
			TStack tStack2 = new TStack();
			do
			{
				string text2 = str.Substring(num, 1);
				if (IsNum(text2))
				{
					text += text2;
					if (num == str.Length - 1 || !IsNum(str.Substring(num + 1, 1)))
					{
						tStack.Push(text);
					}
				}
				else
				{
					text = "";
					if (tStack2.IsEmpty())
					{
						tStack2.Push(text2);
					}
					else
					{
						while (!tStack2.IsEmpty())
						{
							int level = GetLevel(text2);
							object top = tStack2.GetTop();
							if (level > GetLevel(((top != null) ? top.ToString() : null) ?? ""))
							{
								break;
							}
							int num2 = Calculate(tStack, tStack2);
							tStack.Push(num2);
						}
						tStack2.Push(text2);
					}
				}
				num++;
			}
			while (num != str.Length);
			while (!tStack2.IsEmpty())
			{
				int num3 = Calculate(tStack, tStack2);
				tStack.Push(num3);
			}
			object obj = tStack.Pop();
			double x = double.Parse(((obj != null) ? obj.ToString() : null) ?? "");
			return ((int)MathUtils.Round(x)).ToString() ?? "";
		}

		public static bool IsExpression(string str)
		{
			Regex regex = new Regex("^[0-9\\+\\-\\*\\/]+$");
			return !string.IsNullOrEmpty(regex.Match(str).Value);
		}

		public static string ReplaceVariable(string str, int[] ar)
		{
			char[] array = new char[4] { 'X', 'Y', 'V', 'W' };
			return str.Replace(array[0].ToString() ?? "", ar[0].ToString() ?? "").Replace(array[1].ToString() ?? "", ar[1].ToString() ?? "").Replace(array[2].ToString() ?? "", ar[2].ToString() ?? "")
				.Replace(array[3].ToString() ?? "", ar[3].ToString() ?? "");
		}

		public static bool IsNum(string ch)
		{
			switch (ch)
			{
			default:
				if (!(ch == "/"))
				{
					return true;
				}
				goto case "+";
			case "+":
			case "-":
			case "*":
				return false;
			}
		}

		public static int GetLevel(string oper)
		{
			if ("*".Equals(oper) || "/".Equals(oper))
			{
				return 1;
			}
			return 0;
		}

		public static int Calculate(TStack numStatck, TStack operStatck)
		{
			int num = 0;
			if (numStatck.GetTop() != null)
			{
				object obj = numStatck.Pop();
				num = int.Parse(((obj != null) ? obj.ToString() : null) ?? "");
			}
			int num2 = 0;
			if (numStatck.GetTop() != null)
			{
				object obj2 = numStatck.Pop();
				num2 = int.Parse(((obj2 != null) ? obj2.ToString() : null) ?? "");
			}
			string text = "";
			if (operStatck.GetTop() != null)
			{
				object obj3 = operStatck.Pop();
				text = ((obj3 != null) ? obj3.ToString() : null) ?? "";
			}
			int result = 0;
			switch (text.Substring(0, 1))
			{
			case "+":
				result = num + num2;
				break;
			case "-":
				result = num2 - num;
				break;
			case "*":
				result = num * num2;
				break;
			case "/":
				result = num2 / num;
				break;
			}
			return result;
		}

		public static Point2 GetPlayerEyesAngle(ComponentPlayer componentPlayer)
		{
			return DirectionToEyes(GetPlayerEyesDirection(componentPlayer));
		}

		public static Vector3 GetPlayerEyesDirection(ComponentPlayer componentPlayer)
		{
			return componentPlayer.ComponentCreatureModel.EyeRotation.GetForwardVector();
		}

		public static Vector3 GetPlayerBodyDirection(ComponentPlayer componentPlayer)
		{
			return componentPlayer.ComponentBody.Matrix.Forward;
		}

		public static Point3 GetBodyPoint(ComponentBody componentBody)
		{
			int x = (int)MathUtils.Floor(componentBody.Position.X);
			int y = (int)MathUtils.Floor(componentBody.Position.Y);
			int z = (int)MathUtils.Floor(componentBody.Position.Z);
			return new Point3(x, y, z);
		}

		public static Vector3 EyesToDirection(Point2 eyes)
		{
			float x = (float)(eyes.X - 180) / 180f * 3.14f;
			float x2 = (float)(eyes.Y - 90) / 180f * 3.14f;
			float y = MathUtils.Sin(x2);
			float z = (0f - MathUtils.Cos(x)) * MathUtils.Cos(x2);
			float x3 = (0f - MathUtils.Sin(x)) * MathUtils.Cos(x2);
			Vector3 vector = new Vector3(x3, y, z);
			return vector / vector.Length();
		}

		public static Point2 DirectionToEyes(Vector3 direction)
		{
			float num = MathUtils.Asin(direction.Y / direction.Length());
			float num2 = MathUtils.Acos(direction.Z / direction.XZ.Length());
			int num3 = (int)(num2 * 180f / 3.14f) + 1;
			if (direction.X < 0f)
			{
				num3 = 360 - num3;
			}
			int y = (int)(num * 180f / 3.14f) + 90;
			return new Point2(num3, y);
		}

		public static Point2 EyesAdd(Point2 eyes1, Point2 eyes2)
		{
			Point2 result = eyes1 + eyes2;
			result.X %= 360;
			if (result.Y > 180)
			{
				result.Y = 180;
			}
			if (result.Y < 0)
			{
				result.Y = 0;
			}
			return result;
		}

		public static string GetCommandPath()
		{
			string text = ((Environment.CurrentDirectory == "/") ? "android:SurvivalCraft2.3" : "app:");
			string text2 = Storage.CombinePaths(text, "Command");
			if (!Storage.DirectoryExists(text2))
			{
				Storage.CreateDirectory(text2);
			}
			return text2;
		}

		public static string GetCommandResPathName(string pathName)
		{
			if (GameManager.m_worldInfo != null)
			{
				string directoryName = GameManager.m_worldInfo.DirectoryName;
				if (Storage.FileExists(Storage.CombinePaths(directoryName, pathName)))
				{
					return Storage.CombinePaths(directoryName, pathName);
				}
			}
			string text = ((Environment.CurrentDirectory == "/") ? "android:SurvivalCraft2.3" : "app:");
			string text2 = Storage.CombinePaths(text, "Command");
			if (!Storage.DirectoryExists(text2))
			{
				Storage.CreateDirectory(text2);
			}
			return Storage.CombinePaths(text2, pathName);
		}

		public static void DeleteAllFile(string path)
		{
			foreach (string item in Storage.ListFileNames(path))
			{
				Storage.DeleteFile(Storage.CombinePaths(path, item));
			}
			foreach (string item2 in Storage.ListDirectoryNames(path))
			{
				DeleteAllFile(Storage.CombinePaths(path, item2));
			}
		}

		public static void DeleteAllDirectory(string path)
		{
			foreach (string item in Storage.ListDirectoryNames(path))
			{
				DeleteAllDirectory(Storage.CombinePaths(path, item));
			}
			if (Storage.ListDirectoryNames(path).Count() == 0)
			{
				Storage.DeleteDirectory(path);
			}
		}

		public static void DeleteAllDirectoryAndFile(string path)
		{
			DeleteAllFile(path);
			DeleteAllDirectory(path);
		}

		public static object Raycast(Ray3 ray, ComponentMiner componentMiner)
		{
			float reach = 500f;
			Vector3 creaturePosition = componentMiner.ComponentCreature.ComponentCreatureModel.EyePosition;
			Vector3 start = ray.Position;
			Vector3 direction = Vector3.Normalize(ray.Direction);
			Vector3 end = ray.Position + direction * 500f;
			Point3 point = Terrain.ToCell(start);
			BodyRaycastResult? bodyRaycastResult = componentMiner.m_subsystemBodies.Raycast(start, end, 0.35f, delegate(ComponentBody body, float distance)
			{
				bool flag = Vector3.DistanceSquared(start + distance * direction, creaturePosition) <= reach * reach;
				bool flag2 = body.Entity != componentMiner.Entity && !body.IsChildOfBody(componentMiner.ComponentCreature.ComponentBody) && !componentMiner.ComponentCreature.ComponentBody.IsChildOfBody(body);
				bool flag3 = Vector3.Dot(Vector3.Normalize(body.BoundingBox.Center() - start), direction) > 0.7f;
				return (flag && flag2 && flag3) ? true : false;
			});
			MovingBlocksRaycastResult? movingBlocksRaycastResult = componentMiner.m_subsystemMovingBlocks.Raycast(start, end, true);
			TerrainRaycastResult? terrainRaycastResult = componentMiner.m_subsystemTerrain.Raycast(start, end, true, true, (int value, float distance) => true);
			float num = (bodyRaycastResult.HasValue ? bodyRaycastResult.Value.Distance : float.PositiveInfinity);
			float num2 = (movingBlocksRaycastResult.HasValue ? movingBlocksRaycastResult.Value.Distance : float.PositiveInfinity);
			float num3 = (terrainRaycastResult.HasValue ? terrainRaycastResult.Value.Distance : float.PositiveInfinity);
			if (num < num2 && num < num3)
			{
				return bodyRaycastResult.Value;
			}
			if (num2 < num && num2 < num3)
			{
				return movingBlocksRaycastResult.Value;
			}
			if (num3 < num && num3 < num2)
			{
				return terrainRaycastResult.Value;
			}
			return new Ray3(start, direction);
		}
	}
}
