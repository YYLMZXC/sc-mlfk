using System;
using System.Collections.Generic;
using Engine;
using Engine.Graphics;
using GameEntitySystem;
using TemplatesDatabase;
using Game;
namespace Mlfk
{
	public class EntityInfoManager
	{
		public static string[][] ModelTypes = new string[5][]
		{
			new string[6] { "Body", "Head", "Leg1", "Leg2", "Hand1", "Hand2" },
			new string[7] { "Body", "#Neck", "Head", "Leg1", "Leg2", "Leg3", "Leg4" },
			new string[7] { "Body", "Neck", "Head", "Leg1", "Leg2", "#Wing1", "#Wing2" },
			new string[5] { "Body", "#Neck", "Head", "Leg1", "Leg2" },
			new string[4] { "Body", "Tail1", "Tail2", "#Jaw" }
		};

		public static Dictionary<string, EntityInfo> EntityInfos = new Dictionary<string, EntityInfo>();

		public static EntityInfo GetEntityInfo(string name)
		{
			EntityInfo value;
			if (EntityInfos.TryGetValue(name, out value))
			{
				return value;
			}
			return null;
		}

		public static void SetEntityInfos()
		{
			EntityInfo entityInfo = new EntityInfo
			{
				KeyName = "player",
				EntityName = "MalePlayer",
				DisplayName = "玩家",
				Model = "Models/HumanMale",
				Texture = "Textures/Creatures/HumanMale1"
			};
			EntityInfo entityInfo2 = new EntityInfo
			{
				KeyName = "boat",
				EntityName = "Boat",
				DisplayName = "船",
				Model = "Models/Boat",
				Texture = "Textures/Boat"
			};
			EntityInfos.Add(entityInfo.KeyName, entityInfo);
			EntityInfos.Add(entityInfo2.KeyName, entityInfo2);
			foreach (ValuesDictionary entitiesValuesDictionary in DatabaseManager.EntitiesValuesDictionaries)
			{
				try
				{
					string text = entitiesValuesDictionary.DatabaseObject.Name.ToLower();
					if (text == "maleplayer" || text == "femaleplayer")
					{
						continue;
					}
					ValuesDictionary valuesDictionary = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentCreature));
					if (valuesDictionary != null)
					{
						string text2 = valuesDictionary.GetValue<string>("DisplayName");
						if (text2.StartsWith("[") && text2.EndsWith("]"))
						{
							string[] array = text2.Substring(1, text2.Length - 2).Split(new string[1] { ":" }, StringSplitOptions.RemoveEmptyEntries);
							text2 = LanguageControl.GetDatabase("DisplayName", array[1]);
						}
						if (!string.IsNullOrEmpty(text2))
						{
							ValuesDictionary valuesDictionary2 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary, typeof(ComponentCreatureModel));
							EntityInfo entityInfo3 = new EntityInfo
							{
								KeyName = entitiesValuesDictionary.DatabaseObject.Name.ToLower(),
								EntityName = entitiesValuesDictionary.DatabaseObject.Name,
								DisplayName = text2,
								Model = valuesDictionary2.GetValue<string>("ModelName"),
								Texture = valuesDictionary2.GetValue<string>("TextureOverride")
							};
							EntityInfos.Add(entityInfo3.KeyName, entityInfo3);
						}
					}
				}
				catch
				{
				}
			}
			foreach (ValuesDictionary entitiesValuesDictionary2 in DatabaseManager.EntitiesValuesDictionaries)
			{
				try
				{
					if (entitiesValuesDictionary2.DatabaseObject.Name == "Boat")
					{
						continue;
					}
					ValuesDictionary valuesDictionary3 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary2, typeof(ComponentDamage));
					if (valuesDictionary3 != null)
					{
						ValuesDictionary valuesDictionary4 = DatabaseManager.FindValuesDictionaryForComponent(entitiesValuesDictionary2, typeof(ComponentModel));
						if (valuesDictionary4 != null)
						{
							EntityInfo entityInfo4 = new EntityInfo
							{
								KeyName = entitiesValuesDictionary2.DatabaseObject.Name.ToLower(),
								EntityName = entitiesValuesDictionary2.DatabaseObject.Name,
								DisplayName = entitiesValuesDictionary2.DatabaseObject.Name,
								Model = valuesDictionary4.GetValue<string>("ModelName"),
								Texture = valuesDictionary4.GetValue<string>("TextureOverride")
							};
							EntityInfos.Add(entityInfo4.KeyName, entityInfo4);
						}
					}
				}
				catch
				{
				}
			}
		}

		public static string GetModelType(Model model)
		{
			List<string> list = new List<string>();
			foreach (ModelMesh mesh in model.Meshes)
			{
				list.Add(mesh.Name);
			}
			for (int i = 0; i < ModelTypes.Length; i++)
			{
				bool flag = true;
				string[] array = ModelTypes[i];
				foreach (string text in array)
				{
					if (!text.StartsWith("#") && !list.Contains(text))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					switch (i)
					{
					case 0:
						return "HumanModel";
					case 1:
						return "FourLeggedModel";
					case 2:
						return "BirdModel";
					case 3:
						return "FlightlessBirdModel";
					case 4:
						return "FishModel";
					}
				}
			}
			return "OtherModel";
		}

		public static string GetModelType(string obj)
		{
			Entity entity = DatabaseManager.CreateEntity(GameManager.Project, GetEntityName(obj), true);
			ComponentModel componentModel = entity.FindComponent<ComponentModel>();
			if (componentModel != null)
			{
				return componentModel.GetType().Name.Replace("Component", "");
			}
			return "OtherModel";
		}

		public static string GetModelTypeDisplayName(string modelName)
		{
			switch (modelName)
			{
			case "HumanModel":
				return "人模型";
			case "FourLeggedModel":
				return "四脚动物模型";
			case "BirdModel":
				return "鸟模型";
			case "FlightlessBirdModel":
				return "不飞鸟模型";
			case "FishModel":
				return "鱼模型";
			default:
				return "其他模型";
			}
		}

		public static string GetEntityName(string obj)
		{
			EntityInfo entityInfo = GetEntityInfo(obj);
			return (entityInfo != null) ? entityInfo.EntityName : obj;
		}

		public static void ChangeModelDisplay(ref ModelWidget modelWidget, string model, string texture)
		{
			modelWidget.Model = ContentManager.Get<Model>(model);
			modelWidget.TextureOverride = ContentManager.Get<Texture2D>(texture);
			Matrix[] absoluteTransforms = new Matrix[modelWidget.Model.Bones.Count];
			modelWidget.Model.CopyAbsoluteBoneTransformsTo(absoluteTransforms);
			BoundingBox boundingBox = modelWidget.Model.CalculateAbsoluteBoundingBox(absoluteTransforms);
			float x = MathUtils.Max(boundingBox.Size().X, 1.4f * boundingBox.Size().Y, boundingBox.Size().Z);
			modelWidget.ViewPosition = new Vector3(boundingBox.Center().X, 1.5f, boundingBox.Center().Z) + 2.6f * MathUtils.Pow(x, 0.75f) * new Vector3(-1f, 0f, -1f);
			modelWidget.ViewTarget = boundingBox.Center();
			modelWidget.ViewFov = 0.3f;
			modelWidget.AutoRotationVector = Vector3.Zero;
			float num = MathUtils.Clamp(boundingBox.Size().XZ.Length() / boundingBox.Size().Y, 1f, 1.5f);
			modelWidget.Size = new Vector2(modelWidget.Size.Y * num, modelWidget.Size.Y);
		}
	}
}
