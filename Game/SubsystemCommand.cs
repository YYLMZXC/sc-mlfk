using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

namespace Game
{
    public class SubsystemCommand : Subsystem
    {
        private static Dictionary<string, Func<CommandData, SubmitResult>> m_functions = new Dictionary<string, Func<CommandData, SubmitResult>>();

        private static Dictionary<string, Func<CommandData, SubmitResult>> m_conditions = new Dictionary<string, Func<CommandData, SubmitResult>>();

        public ComponentPlayer m_componentPlayer;

        public GameMode m_gameMode;

        public bool m_canWorking = true;

        public static bool MessageTips;

        public static bool ChartletDraw;

        public static bool InteractEnable;

        public override void Load(ValuesDictionary valuesDictionary)
        {
            if (!valuesDictionary.ContainsKey("Class"))
            {
                return;
            }

            string value = valuesDictionary.GetValue<string>("Class");
            if (value != "Game.SubsystemCommand")
            {
                return;
            }

            MessageTips = valuesDictionary.GetValue<bool>("MessageTips");
            ChartletDraw = valuesDictionary.GetValue<bool>("ChartletDraw");
            InteractEnable = valuesDictionary.GetValue<bool>("InteractEnable");
            AddFunction("moremode", delegate (CommandData commandData)
            {
                if (!(commandData.Type == "default"))
                {
                    if (commandData.Type == "notips")
                    {
                        bool flag = (bool)commandData.GetValue("con");
                        MessageTips = !flag;
                    }
                    else if (commandData.Type == "nochartlet")
                    {
                        bool flag2 = (bool)commandData.GetValue("con");
                        ChartletDraw = !flag2;
                    }
                    else if (commandData.Type == "nointeract")
                    {
                        bool flag3 = (bool)commandData.GetValue("con");
                        InteractEnable = !flag3;
                    }
                }

                return SubmitResult.Success;
            });
        }

        public override void Save(ValuesDictionary valuesDictionary)
        {
            if (valuesDictionary.ContainsKey("Class"))
            {
                string value = valuesDictionary.GetValue<string>("Class");
                if (!(value != "Game.SubsystemCommand"))
                {
                    valuesDictionary.SetValue("MessageTips", MessageTips);
                    valuesDictionary.SetValue("ChartletDraw", ChartletDraw);
                    valuesDictionary.SetValue("InteractEnable", InteractEnable);
                }
            }
        }

        public override void OnEntityAdded(Entity entity)
        {
            ComponentPlayer componentPlayer = entity.FindComponent<ComponentPlayer>();
            if (componentPlayer != null)
            {
                m_componentPlayer = componentPlayer;
                m_gameMode = m_componentPlayer.m_subsystemGameInfo.WorldSettings.GameMode;
            }
        }

        public override void Dispose()
        {
            m_functions.Clear();
            m_conditions.Clear();
        }

        public void AddFunction(string name, Func<CommandData, SubmitResult> action)
        {
            if (!m_functions.ContainsKey(name))
            {
                m_functions.Add(name, action);
                return;
            }

            Dictionary<string, Func<CommandData, SubmitResult>> functions = m_functions;
            functions[name] = (Func<CommandData, SubmitResult>)Delegate.Combine(functions[name], action);
        }

        public void AddCondition(string name, Func<CommandData, SubmitResult> action)
        {
            if (!m_conditions.ContainsKey(name))
            {
                m_conditions.Add(name, action);
                return;
            }

            Dictionary<string, Func<CommandData, SubmitResult>> conditions = m_conditions;
            conditions[name] = (Func<CommandData, SubmitResult>)Delegate.Combine(conditions[name], action);
        }

        public SubmitResult Submit(string name, CommandData commandData, bool Judge)
        {
            if (!m_canWorking)
            {
                return SubmitResult.Fail;
            }

            Dictionary<string, Func<CommandData, SubmitResult>> dictionary = ((!Judge) ? m_functions : m_conditions);
            if (dictionary.TryGetValue(name, out var value))
            {
                if (!commandData.Valid)
                {
                    return SubmitResult.Invalid;
                }

                if (commandData.OutRange)
                {
                    return SubmitResult.OutRange;
                }

                try
                {
                    return value(commandData);
                }
                catch (Exception ex)
                {
                    Log.Warning("CommandManager:" + ex.Message);
                    return SubmitResult.Exception;
                }
            }

            return SubmitResult.NoFound;
        }

        public void ShowSubmitTips(string tip, bool onlyError = false, SubmitResult result = SubmitResult.Success, CommandData commandData = null)
        {
            if (!MessageTips)
            {
                return;
            }

            if (onlyError)
            {
                string text = $";\n发生错误的命令方块位置:({commandData.Position.X},{commandData.Position.Y},{commandData.Position.Z})";
                switch (result)
                {
                    case SubmitResult.Exception:
                        m_componentPlayer?.ComponentGui.DisplaySmallMessage("错误:提交的" + commandData.Name + "指令发生异常，请重新编辑" + text, Color.Yellow, blinking: false, playNotificationSound: false);
                        break;
                    case SubmitResult.NoFound:
                        m_componentPlayer?.ComponentGui.DisplaySmallMessage("错误:提交的" + commandData.Name + "指令不存在，请核对指令名称" + text, Color.Yellow, blinking: false, playNotificationSound: false);
                        break;
                    case SubmitResult.Invalid:
                        m_componentPlayer?.ComponentGui.DisplaySmallMessage("错误:提交的" + commandData.Name + "指令存在问题，请重新编辑" + text, Color.Yellow, blinking: false, playNotificationSound: false);
                        break;
                    case SubmitResult.OutRange:
                        m_componentPlayer?.ComponentGui.DisplaySmallMessage("错误:提交的" + commandData.Name + "指令存在参数超出范围，请纠正数值" + text, Color.Yellow, blinking: false, playNotificationSound: false);
                        break;
                    case SubmitResult.Limit:
                        m_componentPlayer?.ComponentGui.DisplaySmallMessage("错误:挑战或残酷模式不支持" + commandData.Name + "指令" + text, Color.Yellow, blinking: false, playNotificationSound: false);
                        break;
                }
            }
            else if (result == SubmitResult.Success)
            {
                m_componentPlayer?.ComponentGui.DisplaySmallMessage("提示:" + tip, Color.Yellow, blinking: false, playNotificationSound: false);
            }
        }

        public void ShowEditedTips(string tip)
        {
            if (!string.IsNullOrEmpty(tip))
            {
                m_componentPlayer?.ComponentGui.DisplaySmallMessage("提示：" + tip, Color.Yellow, blinking: false, playNotificationSound: false);
            }
        }
    }
}