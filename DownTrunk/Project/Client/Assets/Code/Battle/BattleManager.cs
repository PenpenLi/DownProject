using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MS
{
	public class BattleManager : MonoBehaviour
	{
		public Transform BattleRootTran;
		public Transform BattlePoorTran;

		public int RoomId		{ get; set; }
		public int Frequency	{ get; set; }
		public int Stairs		{ get; set; }

		public List<BattleField> m_lstFields = new List<BattleField>();
		public Dictionary<int, BattleRoleBase> m_dicRoles = new Dictionary<int, BattleRoleBase>();
		public BattleRoleM m_RoleM;

		private System.Random _rand;
		private List<BattleFieldData> _lstFieldData = new List<BattleFieldData>();

		private static BattleManager _inst;
		public static BattleManager GetInst()
		{
			return _inst;
		}

		private void Awake()
		{
			_inst = this;
		}

		private void OnDestroy()
		{
			_inst = null;
		}

		public void Load(int roomId, int seed, int frequency, int stairs, List<BattlePlayer> others)
		{
			RoomId = roomId;
			Frequency = frequency;
			Stairs = stairs;
			_rand = new System.Random(seed);
			LoadFieldData();
			BattleField field = ResourceLoader.LoadAssetAndInstantiate("Prefab/BattleFiled", BattleRootTran, PositionMgr.vecFieldPosM).GetComponent<BattleField>();
			field.SceneId = RoleData.CurScene;
			m_lstFields.Add(field);
			m_lstFields[0].Load();
			m_RoleM = ResourceLoader.LoadAssetAndInstantiate("Prefab/BattleRoleM", m_lstFields[0].Foreground).GetComponent<BattleRoleM>();
			for(int i = 0; i < others.Count; ++i)
			{
				field = ResourceLoader.LoadAssetAndInstantiate("Prefab/BattleFiled", BattleRootTran, PositionMgr.vecFieldPosE).GetComponent<BattleField>();
				field.SceneId = others[i].SceneId;
				m_lstFields.Add(field);
				m_lstFields[i + 1].Load();
				m_dicRoles.Add(others[i].PlayerId, ResourceLoader.LoadAssetAndInstantiate("Prefab/BattleRoleE", m_lstFields[i + 1].Foreground).GetComponent<BattleRoleBase>());
			}
			CommonCommand.ExecuteLongBattle(Client2ServerList.GetInst().C2S_BATTLE_LOADED, new ArrayList(){ });
		}

		private void LoadFieldData()
		{
			_lstFieldData.Clear();
			float x, y, lastY = 0;
			int type;

			BattleFieldData data = new BattleFieldData(0, 0, 0);
			_lstFieldData.Add(data);
			for (int i = 0; i < Stairs; ++i)
			{
				x = _rand.Next(-5, 6) / 2f;
				y = lastY - _rand.Next(2, 7) / 2f;
				type = _rand.Next(0, 4);
				lastY = y;

				data = new BattleFieldData(x, y, type);
				_lstFieldData.Add(data);
			}
		}

		public BattleFieldData GetFieldData(int index)
		{
			return _lstFieldData[index];
		}

		public void SetFieldPos(int frame)
		{
			for(int i = 0; i < m_lstFields.Count; ++i)
				m_lstFields[i].SetPos(frame * Frequency * 0.001f);
		}

		public void SetRolePos(int roleId, float x, float y)
		{
			if(RoleData.RoleID != roleId)
				m_dicRoles[roleId].SetPos(x, y);
		}

		public void RemovePlat(PlatBase plat)
		{
			m_lstFields[0].RemovePlat(plat);
		}
	}
}
