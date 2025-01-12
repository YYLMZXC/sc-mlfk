using System;
using System.Collections.Generic;
using Engine;
using GameEntitySystem;
using TemplatesDatabase;

using Game;

namespace Mlfk
{
    public class SubsystemCommandExt : Subsystem, IUpdateable
    {
        public Dictionary<string, Dictionary<string, object>> m_creatureDataChange = new Dictionary<string, Dictionary<string, object>>();

        public Dictionary<int, SubsystemBlockBehavior[]> m_originBehaviorsById = new Dictionary<int, SubsystemBlockBehavior[]>();

        public SubsystemCommand subsystemCommand;

        public SubsystemBodies subsystemBodies;

        public static bool BlockDataChange;

        public static bool ClothesDataChange;

        public static Action LoadAction;

        public UpdateOrder UpdateOrder => UpdateOrder.Default;

        public void Update(float dt)
        {
        }

        public override void OnEntityAdded(Entity entity)
        {
            if (m_creatureDataChange.Count <= 0)
            {
                return;
            }

            foreach (string key in m_creatureDataChange.Keys)
            {
                if (!(key == entity.ValuesDictionary.DatabaseObject.Name.ToLower()))
                {
                    continue;
                }

                ComponentCreature componentCreature = entity.FindComponent<ComponentCreature>();
                {
                    foreach (string key2 in m_creatureDataChange[key].Keys)
                    {
                        object obj = m_creatureDataChange[key][key2];
                        switch (key2)
                        {
                            case "box-size":
                                componentCreature.ComponentBody.BoxSize = (Vector3)obj / 10f;
                                break;
                            case "air-drag":
                                componentCreature.ComponentBody.AirDrag = (Vector2)obj / 10f;
                                break;
                            case "water-drag":
                                componentCreature.ComponentBody.WaterDrag = (Vector2)obj / 10f;
                                break;
                            case "mass":
                                componentCreature.ComponentBody.Mass = (float)(int)obj / 10f;
                                break;
                            case "density":
                                componentCreature.ComponentBody.Density = (int)obj;
                                break;
                            case "air-capacity":
                                componentCreature.ComponentHealth.AirCapacity = (int)obj;
                                break;
                            case "health":
                                componentCreature.ComponentHealth.Health = (float)(int)obj / 100f;
                                break;
                            case "attack-power":
                                componentCreature.Entity.FindComponent<ComponentMiner>(throwOnError: true).AttackPower = (int)obj;
                                break;
                            case "attack-resilience":
                                componentCreature.ComponentHealth.AttackResilience = (float)(int)obj / 10f;
                                break;
                            case "fall-resilience":
                                componentCreature.ComponentHealth.FallResilience = (float)(int)obj / 10f;
                                break;
                            case "corpse-duration":
                                componentCreature.ComponentHealth.CorpseDuration = (float)(int)obj / 10f;
                                break;
                            case "stun-time":
                                componentCreature.ComponentLocomotion.StunTime = (float)(int)obj / 10f;
                                break;
                            case "jump-order":
                                componentCreature.ComponentLocomotion.JumpOrder = (float)(int)obj / 10f;
                                break;
                            case "jump-speed":
                                componentCreature.ComponentLocomotion.JumpSpeed = (float)(int)obj / 10f;
                                break;
                            case "swim-speed":
                                componentCreature.ComponentLocomotion.SwimSpeed = (float)(int)obj / 10f;
                                break;
                            case "turn-speed":
                                componentCreature.ComponentLocomotion.TurnSpeed = (float)(int)obj / 10f;
                                break;
                            case "walk-speed":
                                componentCreature.ComponentLocomotion.WalkSpeed = (float)(int)obj / 10f;
                                break;
                            case "fly-speed":
                                componentCreature.ComponentLocomotion.FlySpeed = (float)(int)obj / 10f;
                                break;
                            case "is-invulnerable":
                                componentCreature.ComponentHealth.IsInvulnerable = (bool)obj;
                                break;
                        }
                    }

                    break;
                }
            }
        }

        public override void Load(ValuesDictionary valuesDictionary)
        {
            subsystemCommand = base.Project.FindSubsystem<SubsystemCommand>();
            subsystemBodies = base.Project.FindSubsystem<SubsystemBodies>();
            m_originBehaviorsById.Clear();
            if (BlockDataChange)
            {
                foreach (ModEntity mod in ModsManager.ModList)
                {
                    mod.LoadBlocksData();
                }

                BlockDataChange = false;
            }

            if (ClothesDataChange)
            {
                ((ClothingBlock)BlocksManager.Blocks[203]).Initialize();
                ClothesDataChange = false;
            }

            subsystemCommand.AddFunction("blockdata", delegate (CommandData commandData)
            {
                BlockDataChange = true;
                return ChangeBlockData(commandData);
            });
            subsystemCommand.AddFunction("creaturedata", delegate (CommandData commandData)
            {
                SubmitResult submitResult = ChangeCreatureData(commandData);
                if (submitResult == SubmitResult.Success && commandData.Type != "default")
                {
                    object value = null;
                    object value2 = commandData.GetValue("vec3");
                    object value3 = commandData.GetValue("vec2");
                    object value4 = commandData.GetValue("v");
                    object value5 = commandData.GetValue("con");
                    if (value2 != null)
                    {
                        value = value2;
                    }
                    else if (value3 != null)
                    {
                        value = value3;
                    }
                    else if (value4 != null)
                    {
                        value = value4;
                    }
                    else if (value5 != null)
                    {
                        value = value5;
                    }

                    string[] array = new string[18]
                    {
                        "box-size", "air-drag", "water-drag", "mass", "density", "air-capacity", "health", "attack-power", "attack-resilience", "fall-resilience",
                        "corpse-duration", "stun-time", "jump-order", "jump-speed", "swim-speed", "turn-speed", "walk-speed", "fly-speed"
                    };
                    string[] array2 = array;
                    foreach (string text in array2)
                    {
                        if (text == commandData.Type)
                        {
                            string key = (string)commandData.GetValue("obj");
                            if (!m_creatureDataChange.ContainsKey(key))
                            {
                                m_creatureDataChange[key] = new Dictionary<string, object>();
                            }

                            m_creatureDataChange[key][text] = value;
                            break;
                        }
                    }
                }

                return submitResult;
            });
            subsystemCommand.AddFunction("clothesdata", delegate (CommandData commandData)
            {
                ClothesDataChange = true;
                return ChangeClothesdata(commandData);
            });
            if (LoadAction != null)
            {
                LoadAction();
            }
        }

        public SubmitResult ChangeBlockData(CommandData commandData)
        {
            if (commandData.Type == "default")
            {
                return SubmitResult.Success;
            }

            int num = (int)commandData.GetValue("id");
            Block block = BlocksManager.Blocks[num];
            if (block is AirBlock)
            {
                subsystemCommand.ShowSubmitTips("无效的方块id:" + num);
                return SubmitResult.Fail;
            }

            if (commandData.Type == "behaviors")
            {
                string text = (string)commandData.GetValue("opt");
                try
                {
                    if (text.ToLower() == "null")
                    {
                        if (m_originBehaviorsById.TryGetValue(num, out var _))
                        {
                            base.Project.FindSubsystem<SubsystemBlockBehaviors>().m_blockBehaviorsByContents[num] = m_originBehaviorsById[num];
                        }

                        return SubmitResult.Success;
                    }

                    bool flag = false;
                    string[] array = new string[29]
                    {
                        "Throwable", "Wood", "Ivy", "Rot", "BottomSucker", "WaterPlant", "Fence", "ImpactExplosives", "Explosives", "Carpet",
                        "Campfile", "Furniture", "Egg", "Hammer", "Bow", "Crossbow", "Musket", "Arrow", "Bomb", "Fireworks",
                        "Piston", "Grave", "Soil", "Saddle", "Match", "Bucket", "Cactus", "Bullet", "Fertilizer"
                    };
                    string[] array2 = text.Split(new char[1] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < array2.Length; i++)
                    {
                        string[] array3 = array;
                        foreach (string text2 in array3)
                        {
                            if (text2.ToLower() == array2[i].ToLower())
                            {
                                array2[i] = text2 + "BlockBehavior";
                                flag = true;
                                break;
                            }
                        }

                        if (!flag)
                        {
                            subsystemCommand.ShowSubmitTips($"{array2[i]}行为不存在或不可用");
                            return SubmitResult.Fail;
                        }

                        flag = false;
                    }

                    List<SubsystemBlockBehavior> list = new List<SubsystemBlockBehavior>();
                    string[] array4 = array2;
                    foreach (string text3 in array4)
                    {
                        SubsystemBlockBehavior item = base.Project.FindSubsystem<SubsystemBlockBehavior>(text3.Trim(), throwOnError: true);
                        list.Add(item);
                    }

                    if (!m_originBehaviorsById.TryGetValue(num, out var value2))
                    {
                        value2 = base.Project.FindSubsystem<SubsystemBlockBehaviors>().m_blockBehaviorsByContents[num];
                        m_originBehaviorsById[num] = value2;
                    }

                    SubsystemBlockBehavior[] array5 = value2;
                    foreach (SubsystemBlockBehavior item2 in array5)
                    {
                        list.Add(item2);
                    }

                    base.Project.FindSubsystem<SubsystemBlockBehaviors>().m_blockBehaviorsByContents[num] = list.ToArray();
                }
                catch
                {
                    Log.Warning("The illegal behaviors");
                    subsystemCommand.ShowSubmitTips("行为装载发生了未知错误");
                    return SubmitResult.Fail;
                }
            }
            else if (commandData.Type == "food-type")
            {
                string text4 = (string)commandData.GetValue("opt");
                switch (text4.ToLower())
                {
                    case "bread":
                        BlocksManager.Blocks[num].FoodType = FoodType.Bread;
                        break;
                    case "fish":
                        BlocksManager.Blocks[num].FoodType = FoodType.Fish;
                        break;
                    case "fruit":
                        BlocksManager.Blocks[num].FoodType = FoodType.Fruit;
                        break;
                    case "grass":
                        BlocksManager.Blocks[num].FoodType = FoodType.Grass;
                        break;
                    case "meat":
                        BlocksManager.Blocks[num].FoodType = FoodType.Meat;
                        break;
                    case "null":
                        BlocksManager.Blocks[num].FoodType = FoodType.None;
                        break;
                    default:
                        subsystemCommand.ShowSubmitTips("不存在食物类型:" + text4);
                        return SubmitResult.Fail;
                }
            }
            else
            {
                object value3 = commandData.GetValue("con");
                if (value3 != null)
                {
                    bool flag2 = (bool)value3;
                    switch (commandData.Type)
                    {
                        case "is-collidable":
                            BlocksManager.Blocks[num].IsCollidable = flag2;
                            break;
                        case "is-placeable":
                            BlocksManager.Blocks[num].IsPlaceable = flag2;
                            break;
                        case "is-digging-transparent":
                            BlocksManager.Blocks[num].IsDiggingTransparent = flag2;
                            break;
                        case "is-placement-transparent":
                            BlocksManager.Blocks[num].IsPlacementTransparent = flag2;
                            break;
                        case "is-interactive":
                            BlocksManager.Blocks[num].DefaultIsInteractive = flag2;
                            break;
                        case "is-editable":
                            BlocksManager.Blocks[num].IsEditable = flag2;
                            break;
                        case "is-non-duplicable":
                            BlocksManager.Blocks[num].IsNonDuplicable = flag2;
                            break;
                        case "is-gatherable":
                            BlocksManager.Blocks[num].IsGatherable = flag2;
                            break;
                        case "has-collision-behavior":
                            BlocksManager.Blocks[num].HasCollisionBehavior = flag2;
                            break;
                        case "kills-when-stuck":
                            BlocksManager.Blocks[num].KillsWhenStuck = flag2;
                            break;
                        case "is-fluid-blocker":
                            BlocksManager.Blocks[num].IsFluidBlocker = flag2;
                            break;
                        case "is-transparent":
                            BlocksManager.Blocks[num].IsTransparent = flag2;
                            break;
                        case "is-aimable":
                            BlocksManager.Blocks[num].IsAimable = flag2;
                            break;
                        case "is-stickable":
                            BlocksManager.Blocks[num].IsStickable = flag2;
                            break;
                        case "align-to-velocity":
                            BlocksManager.Blocks[num].AlignToVelocity = flag2;
                            break;
                        case "no-auto-jump":
                            BlocksManager.Blocks[num].NoAutoJump = flag2;
                            break;
                        case "no-smooth-rise":
                            BlocksManager.Blocks[num].NoSmoothRise = flag2;
                            break;
                        case "disintegrates-on-hit":
                            BlocksManager.Blocks[num].DisintegratesOnHit = flag2;
                            break;
                        case "explosion-incendiary":
                            BlocksManager.Blocks[num].DefaultExplosionIncendiary = flag2;
                            break;
                        case "is-explosion-transparent":
                            BlocksManager.Blocks[num].IsExplosionTransparent = flag2;
                            break;
                        default:
                            return SubmitResult.Fail;
                    }

                    return SubmitResult.Success;
                }

                object value4 = commandData.GetValue("v");
                if (value4 != null)
                {
                    int num2 = (int)value4;
                    float num3 = (float)num2 / 10f;
                    switch (commandData.Type)
                    {
                        case "icon-view-scale":
                            BlocksManager.Blocks[num].DefaultIconViewScale = num3;
                            break;
                        case "first-person-scale":
                            BlocksManager.Blocks[num].FirstPersonScale = num3;
                            break;
                        case "in-hand-scale":
                            BlocksManager.Blocks[num].InHandScale = num3;
                            break;
                        case "shadow-strength":
                            BlocksManager.Blocks[num].DefaultShadowStrength = num2;
                            break;
                        case "light-attenuation":
                            BlocksManager.Blocks[num].LightAttenuation = num2;
                            break;
                        case "emitted-light-amount":
                            BlocksManager.Blocks[num].DefaultEmittedLightAmount = num2;
                            break;
                        case "object-shadow-strength":
                            BlocksManager.Blocks[num].ObjectShadowStrength = num3;
                            break;
                        case "drop-count":
                            BlocksManager.Blocks[num].DefaultDropCount = num2;
                            break;
                        case "experience-count":
                            BlocksManager.Blocks[num].DefaultExperienceCount = num3;
                            break;
                        case "required-tool-level":
                            BlocksManager.Blocks[num].RequiredToolLevel = num2;
                            break;
                        case "max-stacking":
                            BlocksManager.Blocks[num].MaxStacking = num2;
                            break;
                        case "sleep-suitability":
                            BlocksManager.Blocks[num].SleepSuitability = num3;
                            break;
                        case "friction-factor":
                            BlocksManager.Blocks[num].FrictionFactor = num3;
                            break;
                        case "density":
                            BlocksManager.Blocks[num].Density = num3;
                            break;
                        case "fuel-heat-level":
                            BlocksManager.Blocks[num].FuelHeatLevel = num2;
                            break;
                        case "fuel-fire-duration":
                            BlocksManager.Blocks[num].FuelFireDuration = num2;
                            break;
                        case "shovel-power":
                            BlocksManager.Blocks[num].ShovelPower = num3;
                            break;
                        case "quarry-power":
                            BlocksManager.Blocks[num].QuarryPower = num3;
                            break;
                        case "hack-power":
                            BlocksManager.Blocks[num].HackPower = num3;
                            break;
                        case "melee-power":
                            BlocksManager.Blocks[num].DefaultMeleePower = num3;
                            break;
                        case "melee-hit-probability":
                            BlocksManager.Blocks[num].DefaultMeleeHitProbability = num3;
                            break;
                        case "projectile-power":
                            BlocksManager.Blocks[num].DefaultProjectilePower = num3;
                            break;
                        case "tool-level":
                            BlocksManager.Blocks[num].ToolLevel = num2;
                            break;
                        case "player-level-required":
                            BlocksManager.Blocks[num].PlayerLevelRequired = num2;
                            break;
                        case "projectile-speed":
                            BlocksManager.Blocks[num].ProjectileSpeed = num2;
                            break;
                        case "projectile-damping":
                            BlocksManager.Blocks[num].ProjectileDamping = num3;
                            break;
                        case "projectile-tip-offset":
                            BlocksManager.Blocks[num].ProjectileTipOffset = num3;
                            break;
                        case "projectile-stick-probability":
                            BlocksManager.Blocks[num].ProjectileStickProbability = num3;
                            break;
                        case "heat":
                            BlocksManager.Blocks[num].DefaultHeat = num3;
                            break;
                        case "fire-duration":
                            BlocksManager.Blocks[num].FireDuration = num2;
                            break;
                        case "explosion-resilience":
                            BlocksManager.Blocks[num].ExplosionResilience = num2;
                            break;
                        case "explosion-pressure":
                            BlocksManager.Blocks[num].DefaultExplosionPressure = num2;
                            break;
                        case "dig-resilience":
                            BlocksManager.Blocks[num].DigResilience = num3;
                            break;
                        case "projectile-resilience":
                            BlocksManager.Blocks[num].ProjectileResilience = num3;
                            break;
                        case "sickness-probability":
                            BlocksManager.Blocks[num].DefaultSicknessProbability = num3;
                            break;
                        case "rot-period":
                            BlocksManager.Blocks[num].DefaultRotPeriod = num2;
                            break;
                        case "texture-slot":
                            BlocksManager.Blocks[num].DefaultTextureSlot = num2;
                            break;
                        case "drop-content":
                            BlocksManager.Blocks[num].DefaultDropContent = num2;
                            break;
                        case "durability":
                            BlocksManager.Blocks[num].Durability = num2;
                            break;
                        default:
                            return SubmitResult.Fail;
                    }

                    return SubmitResult.Success;
                }

                object value5 = commandData.GetValue("vec3");
                if (value5 != null)
                {
                    Vector3 vector = (Vector3)value5;
                    switch (commandData.Type)
                    {
                        case "icon-block-offset":
                            BlocksManager.Blocks[num].DefaultIconBlockOffset = vector;
                            break;
                        case "icon-view-offset":
                            BlocksManager.Blocks[num].DefaultIconViewOffset = vector;
                            break;
                        case "first-person-offset":
                            BlocksManager.Blocks[num].FirstPersonOffset = vector;
                            break;
                        case "first-person-rotation":
                            BlocksManager.Blocks[num].FirstPersonRotation = vector;
                            break;
                        case "in-hand-offset":
                            BlocksManager.Blocks[num].InHandOffset = vector;
                            break;
                        case "in-hand-rotation":
                            BlocksManager.Blocks[num].InHandRotation = vector;
                            break;
                        default:
                            return SubmitResult.Fail;
                    }

                    return SubmitResult.Success;
                }
            }

            return SubmitResult.Success;
        }

        public SubmitResult ChangeCreatureData(CommandData commandData)
        {
            if (commandData.Type == "default")
            {
                return SubmitResult.Success;
            }

            string text = (string)commandData.GetValue("obj");
            Vector2 vector = new Vector2(subsystemCommand.m_componentPlayer.ComponentBody.Position.X, subsystemCommand.m_componentPlayer.ComponentBody.Position.Z);
            DynamicArray<ComponentBody> dynamicArray = new DynamicArray<ComponentBody>();
            subsystemBodies.FindBodiesInArea(vector - new Vector2(64f), vector + new Vector2(64f), dynamicArray);
            foreach (ComponentBody item in dynamicArray)
            {
                bool flag = item.Entity.FindComponent<ComponentPlayer>() != null && text == "player";
                if (!(text == item.Entity.ValuesDictionary.DatabaseObject.Name.ToLower() || flag))
                {
                    continue;
                }

                ComponentDamage componentDamage = item.Entity.FindComponent<ComponentDamage>();
                if (componentDamage != null)
                {
                    subsystemCommand.ShowSubmitTips("creaturedata指令暂时不支持载具");
                    return SubmitResult.Fail;
                }

                ComponentCreature componentCreature = item.Entity.FindComponent<ComponentCreature>();
                if (componentCreature == null)
                {
                    continue;
                }

                if (commandData.Type.StartsWith("action-"))
                {
                    int num = (int)commandData.GetValue("v");
                    float num2 = (float)num / 10f;
                    ComponentCreatureModel componentCreatureModel = componentCreature.ComponentCreatureModel;
                    if (componentCreatureModel is ComponentFourLeggedModel)
                    {
                        switch (commandData.Type)
                        {
                            case "action-head":
                                ((ComponentFourLeggedModel)componentCreatureModel).m_headAngleY = num2;
                                break;
                            case "action-left-hand":
                                ((ComponentFourLeggedModel)componentCreatureModel).m_legAngle1 = num2;
                                break;
                            case "action-right-hand":
                                ((ComponentFourLeggedModel)componentCreatureModel).m_legAngle2 = num2;
                                break;
                            case "action-left-leg":
                                ((ComponentFourLeggedModel)componentCreatureModel).m_legAngle3 = num2;
                                break;
                            case "action-right-leg":
                                ((ComponentFourLeggedModel)componentCreatureModel).m_legAngle4 = num2;
                                break;
                            default:
                                return SubmitResult.Fail;
                        }
                    }
                    else if (componentCreatureModel is ComponentFlightlessBirdModel)
                    {
                        switch (commandData.Type)
                        {
                            case "action-head":
                                ((ComponentFlightlessBirdModel)componentCreatureModel).m_headAngleY = num2;
                                break;
                            case "action-left-hand":
                                ((ComponentFlightlessBirdModel)componentCreatureModel).m_legAngle1 = num2;
                                break;
                            case "action-right-hand":
                                ((ComponentFlightlessBirdModel)componentCreatureModel).m_legAngle2 = num2;
                                break;
                            case "action-left-leg":
                                ((ComponentFlightlessBirdModel)componentCreatureModel).m_legAngle1 = num2;
                                break;
                            case "action-right-leg":
                                ((ComponentFlightlessBirdModel)componentCreatureModel).m_legAngle2 = num2;
                                break;
                            default:
                                return SubmitResult.Fail;
                        }
                    }
                    else if (componentCreatureModel is ComponentHumanModel)
                    {
                        switch (commandData.Type)
                        {
                            case "action-head":
                                ((ComponentHumanModel)componentCreatureModel).m_headAngles = new Vector2(0f, num2);
                                break;
                            case "action-left-hand":
                                ((ComponentHumanModel)componentCreatureModel).m_handAngles1 = new Vector2(num2, (0f - num2) * 0.1f);
                                break;
                            case "action-right-hand":
                                ((ComponentHumanModel)componentCreatureModel).m_handAngles2 = new Vector2(num2, num2 * 0.1f);
                                break;
                            case "action-left-leg":
                                ((ComponentHumanModel)componentCreatureModel).m_legAngles1 = new Vector2(num2, num2 * 0.1f);
                                break;
                            case "action-right-leg":
                                ((ComponentHumanModel)componentCreatureModel).m_legAngles2 = new Vector2(num2, (0f - num2) * 0.1f);
                                break;
                            default:
                                return SubmitResult.Fail;
                        }
                    }
                    else
                    {
                        if (componentCreatureModel is ComponentBirdModel)
                        {
                            subsystemCommand.ShowSubmitTips("当前指令暂不支持鸟模型");
                            return SubmitResult.Fail;
                        }

                        if (componentCreatureModel is ComponentFishModel)
                        {
                            subsystemCommand.ShowSubmitTips("当前指令暂不支持鱼模型");
                            return SubmitResult.Fail;
                        }
                    }

                    continue;
                }

                object value = commandData.GetValue("vec3");
                if (value != null)
                {
                    Vector3 vector2 = (Vector3)value / 10f;
                    string type = commandData.Type;
                    string text2 = type;
                    if (!(text2 == "box-size"))
                    {
                        if (!(text2 == "velocity"))
                        {
                            return SubmitResult.Fail;
                        }

                        componentCreature.ComponentBody.Velocity = vector2;
                    }
                    else
                    {
                        componentCreature.ComponentBody.BoxSize = vector2;
                    }
                }

                object value2 = commandData.GetValue("vec2");
                if (value2 != null)
                {
                    Vector2 vector3 = (Vector2)value2 / 10f;
                    switch (commandData.Type)
                    {
                        case "look-order":
                            componentCreature.ComponentLocomotion.LookOrder = vector3;
                            break;
                        case "turn-order":
                            componentCreature.ComponentLocomotion.TurnOrder = vector3;
                            break;
                        case "air-drag":
                            componentCreature.ComponentBody.AirDrag = vector3;
                            break;
                        case "water-drag":
                            componentCreature.ComponentBody.WaterDrag = vector3;
                            break;
                        default:
                            return SubmitResult.Fail;
                    }
                }

                object value3 = commandData.GetValue("v");
                if (value3 != null)
                {
                    int num3 = (int)value3;
                    float num4 = (float)num3 / 10f;
                    switch (commandData.Type)
                    {
                        case "mass":
                            componentCreature.ComponentBody.Mass = num4;
                            break;
                        case "density":
                            componentCreature.ComponentBody.Density = num3;
                            break;
                        case "air-capacity":
                            componentCreature.ComponentHealth.AirCapacity = num3;
                            break;
                        case "health":
                            componentCreature.ComponentHealth.Health = (float)num3 / 100f;
                            break;
                        case "attack-power":
                            componentCreature.Entity.FindComponent<ComponentMiner>(throwOnError: true).AttackPower = num3;
                            break;
                        case "attack-resilience":
                            componentCreature.ComponentHealth.AttackResilience = num4;
                            break;
                        case "fall-resilience":
                            componentCreature.ComponentHealth.FallResilience = num4;
                            break;
                        case "corpse-duration":
                            componentCreature.ComponentHealth.CorpseDuration = num4;
                            break;
                        case "stun-time":
                            componentCreature.ComponentLocomotion.StunTime = num4;
                            break;
                        case "jump-order":
                            componentCreature.ComponentLocomotion.JumpOrder = num4;
                            break;
                        case "jump-speed":
                            componentCreature.ComponentLocomotion.JumpSpeed = num4;
                            break;
                        case "swim-speed":
                            componentCreature.ComponentLocomotion.SwimSpeed = num4;
                            break;
                        case "turn-speed":
                            componentCreature.ComponentLocomotion.TurnSpeed = num4;
                            break;
                        case "walk-speed":
                            componentCreature.ComponentLocomotion.WalkSpeed = num4;
                            break;
                        case "fly-speed":
                            componentCreature.ComponentLocomotion.FlySpeed = num4;
                            break;
                        default:
                            return SubmitResult.Fail;
                    }
                }

                object value4 = commandData.GetValue("con");
                if (value4 != null)
                {
                    bool isInvulnerable = (bool)value4;
                    string type2 = commandData.Type;
                    string text3 = type2;
                    if (!(text3 == "is-invulnerable"))
                    {
                        return SubmitResult.Fail;
                    }

                    componentCreature.ComponentHealth.IsInvulnerable = isInvulnerable;
                }
            }

            return SubmitResult.Success;
        }

        public SubmitResult ChangeClothesdata(CommandData commandData)
        {
            return SubmitResult.Success;
        }
    }
}