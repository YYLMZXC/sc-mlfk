using System;
using System.Collections.Generic;
using Engine;
using Game;
namespace Mlfk
{
    public class CommandData
    {
        public Point3 Position;

        public string Line;

        public string Name;

        public string Type = "default";

        public WorkingMode Mode = WorkingMode.Default;

        public CoordinateMode Coordinate = CoordinateMode.Default;

        public Dictionary<string, object> Data = new Dictionary<string, object>();

        public Dictionary<string, string> DataText = new Dictionary<string, string>();

        public bool OutRange = false;

        public bool Valid = true;

        public Dictionary<string, object> DIYPara = new Dictionary<string, object>();

        public CommandData(Point3 position, string line)
        {
            Position = position;
            Line = line;
        }

        public object GetValue(string key)
        {
            if (Data.TryGetValue(key, out var value))
            {
                return value;
            }

            return null;
        }

        public string TrySetValue()
        {
            if (Line == "" || string.IsNullOrEmpty(Line))
            {
                Valid = false;
                return string.Empty;
            }

            try
            {
                Line = Line.Replace("\n", " ");
                string[] array = Line.Split(new char[1] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (!array[0].Contains(":"))
                {
                    array[0] = "cmd:" + array[0];
                }

                string[] array2 = array[0].Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                Name = array2[1];
                Mode = WorkingMode.Default;
                if (array2[0].ToLower() == "if")
                {
                    Mode = WorkingMode.Condition;
                }
                else if (DataHandle.IsContainsVariable(Line))
                {
                    Mode = WorkingMode.Variable;
                }

                for (int i = 0; i < array.Length; i++)
                {
                    if (!array[i].Contains(":") && array[i].Contains("@"))
                    {
                        array[i] = "cd:" + array[i];
                    }

                    string[] array3 = array[i].Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    string text = array3[0].ToLower();
                    string text2 = text;
                    string text3 = text2;
                    if (!(text3 == "type"))
                    {
                        if (text3 == "cd")
                        {
                            if (array3[1].ToLower() == "@c")
                            {
                                Coordinate = CoordinateMode.Command;
                            }
                            else if (array3[1].ToLower() == "@pl")
                            {
                                Coordinate = CoordinateMode.Player;
                            }
                        }
                    }
                    else
                    {
                        Type = InstructionManager.GetCommandType(Name, Mode == WorkingMode.Condition, array3[1].ToLower());
                    }

                    if (!InstructionManager.IsFixedParameter(text))
                    {
                        DataText[text] = array3[1];
                    }
                }

                string[] array4 = array;
                foreach (string text4 in array4)
                {
                    string[] array5 = text4.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                    if (Mode == WorkingMode.Variable && DataHandle.IsContainsVariable(array5[0] + ":" + array5[1]))
                    {
                        Data[array5[0]] = array5[1];
                    }
                    else
                    {
                        SetDataPara(array5[0].ToLower(), array5[1]);
                    }
                }

                if (Mode != WorkingMode.Variable)
                {
                    Instruction instruction = InstructionManager.GetInstruction(Name, Mode == WorkingMode.Condition);
                    if (instruction == null)
                    {
                        Valid = false;
                        return "当前编辑的指令是无效的，请重新编辑";
                    }

                    foreach (string key in Data.Keys)
                    {
                        if (instruction.Ranges.TryGetValue(Type + "$" + key, out var value))
                        {
                            int num = (int)Data[key];
                            if (num < value.X || num > value.Y)
                            {
                                OutRange = true;
                                string parameterName = InstructionManager.GetParameterName(Name, Type, key, Mode == WorkingMode.Condition);
                                return $"{Name}指令的{parameterName}(类型:{Type})的参数范围为{value.X}-{value.Y}，请纠正数值";
                            }
                        }
                    }
                }

                Valid = true;
                return string.Empty;
            }
            catch
            {
                Valid = false;
                if (Mode == WorkingMode.Condition && DataHandle.IsContainsVariable(Line))
                {
                    return "条件判断模式不支持指令变量";
                }

                return "当前编辑的指令是无效的，请重新编辑";
            }
        }

        public void FastSetValue()
        {
            foreach (string key in DataText.Keys)
            {
                if (DataHandle.IsContainsVariable(key + ":" + DataText[key]))
                {
                    try
                    {
                        SetDataPara(key.ToLower(), Data[key].ToString());
                    }
                    catch
                    {
                    }
                }
            }
        }

        public void SetDataPara(string key, string value)
        {
            switch (key)
            {
                case "cmd":
                    return;
                case "if":
                    return;
                case "type":
                    return;
                case "cd":
                    return;
                case "vec2":
                    Data[key] = DataHandle.GetVector2Value(value);
                    return;
                case "vec3":
                    Data[key] = DataHandle.GetVector3Value(value);
                    return;
                case "vec4":
                    Data[key] = DataHandle.GetVector4Value(value);
                    return;
            }

            int result;
            if (key.StartsWith("id"))
            {
                Data[key] = DataHandle.GetNaturalValue(value);
            }
            else if (key.StartsWith("fid"))
            {
                Data[key] = DataHandle.GetNaturalValue(value);
            }
            else if (key.StartsWith("pos"))
            {
                Data[key] = DataHandle.GetPoint3Value(value);
            }
            else if (key.StartsWith("eyes"))
            {
                Data[key] = DataHandle.GetPoint2Value(value);
            }
            else if (key.StartsWith("con"))
            {
                Data[key] = DataHandle.GetBoolValue(value);
            }
            else if (key.StartsWith("color"))
            {
                Data[key] = DataHandle.GetColorValue(value);
            }
            else if (key.StartsWith("text"))
            {
                Data[key] = DataHandle.CharacterEscape(value);
            }
            else if (key.StartsWith("obj"))
            {
                Data[key] = value.ToLower();
            }
            else if (key.StartsWith("time"))
            {
                Data[key] = DataHandle.GetDateTimeValue(value);
            }
            else if (key.StartsWith("fix"))
            {
                Data[key] = DataHandle.CharacterEscape(value);
            }
            else if (key.StartsWith("opt"))
            {
                Data[key] = DataHandle.CharacterEscape(value);
            }
            else if (int.TryParse(value, out result))
            {
                Data[key] = result;
            }
            else
            {
                Data[key] = value;
            }
        }
    }
}