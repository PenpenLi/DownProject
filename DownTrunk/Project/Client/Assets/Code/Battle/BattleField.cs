using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MS
{
	public class BattleField : MonoBehaviour
	{
		public Transform			SpriteMaskTran;
		public Transform			ForegroundTran;
		public Transform			SkillBarTran;
		public TextMeshPro			PlayerIndexText;
		public TextMesh				PlayerNameText;
		public GameObject			FailedTextGo;
		public GameObject[]			HpGos;
		public BattleColliderTop	DisTrigger;

		[HideInInspector]
		public Transform Background;

		private Transform _transform;

		public int				PlayerId	{ get; set; }
		public int				SceneId		{ get; set; }
		public BattleHeroBase	Hero		{ get; set; }

		private bool _bFailed;
		public bool	IsFailed
		{
			get { return _bFailed; }
			set { _bFailed = value; FailedTextGo.SetActive(value); }
				
		}

		private string _sPlayerName;
		public string PlayerName
		{
			get { return _sPlayerName; }
			set { _sPlayerName = value; PlayerNameText.text = value; }
		}

		private int _sPlayerIndex;
		public int PlayerIndex
		{
			get { return _sPlayerIndex; }
			set { _sPlayerIndex = value; PlayerIndexText.text = (value + 1).ToString(); }
		}

		public int MaxHP { get; set; }

		private int _iCurHp;
		public int CurHP
		{
			get { return _iCurHp; }
			set
			{
				_iCurHp = Mathf.Min(value, MaxHP);
				for(int i = 0; i < HpGos.Length; ++i)
					HpGos[i].SetActive(false);

				for(int i = 0; i < _iCurHp; ++i)
					HpGos[i].SetActive(true);

				if(_iCurHp == 0)
				{
					if(BattleManager.GetInst().BattleType == 2 && PlayerId == PlayerData.PlayerId)
					{
						BattleManager.GetInst().m_RoleM.Disable();
						CommonCommand.ExecuteLongBattle(Client2ServerList.GetInst().C2S_BATTLE_HERO_FAILED, new ArrayList() { });
					}
				}
			}
		}

		private Material _matBg;
		private Vector3 _tempPos = new Vector3();
		private int _iCurMinIndex = 0;
		private Dictionary<int, BattlePlat> _dicPlat = new Dictionary<int, BattlePlat>();

		private void Awake()
		{
			_transform = transform;
			DisTrigger.TriggerCbFunc = PlatOutField;
			IsFailed = false;
		}

		public void SetScale(float scale)
		{
			_transform.localScale = new Vector3(scale, scale, 1f);
		}

		public void Load()
		{
			ResourceLoader.LoadAssetAndInstantiate(string.Format("Prefab/Scene/{0}/Nails", SceneId), SpriteMaskTran);
			Background = ResourceLoader.LoadAssetAndInstantiate(string.Format("Prefab/Scene/{0}/Background", SceneId), SpriteMaskTran).GetComponent<Transform>();
			_matBg = Background.GetComponent<SpriteRenderer>().material;
			for (int i = 0; i < 30; ++i)
				AddPlat();
		}

		public void InitData(int playerId, int playerIndex, string playerName, int sceneId, int hp)
		{
			PlayerId	= playerId;
			PlayerIndex	= playerIndex;
			PlayerName	= playerName;
			SceneId		= sceneId;
			MaxHP		= hp;
			CurHP		= hp;
		}

		public void SetHero(BattleHeroBase hero)
		{
			Hero = hero;
			SetMainSkill(Hero.HeroId);
		}

		public void SetPos(float y)
		{
			if(IsFailed)
				return;

			_tempPos.y = y;
			ForegroundTran.localPosition = _tempPos;

			_tempPos.y = y / -30f;
			_matBg.mainTextureOffset = _tempPos;
		}

		public void PlatOutField(BattlePlat plat)
		{
			RemovePlat(plat);
			_iCurMinIndex = plat.Index + 1;
		}

		public void RemovePlat(BattlePlat plat)
		{
			_dicPlat.Remove(plat.Index);
			ResourceMgr.PushBox(SceneId, plat.Type, plat);
			AddPlat();
		}

		private int _iPlatIndex = -1;
		public void AddPlat()
		{
			if(++_iPlatIndex > BattleManager.GetInst().Stairs)
				return;
				
			BattleFieldData field = BattleManager.GetInst().GetFieldData(_iPlatIndex);
			BattlePlat plat = ResourceMgr.PopBox(SceneId, field.Type);
			plat.Index = _iPlatIndex;
			plat.Y = field.Y;
			plat.m_Transform.SetParent(ForegroundTran);
			plat.m_Transform.localPosition = new Vector3(field.X, field.Y, 0f);
			plat.m_Transform.localScale = Vector3.one;
			if(field.Item > 0)
			{
				string itemId = ConfigData.GetValue("Scene_Common", SceneId.ToString(), string.Format("Item{0}", field.Item));
				if(itemId != "-1")
				{
					int id = int.Parse(itemId);
					BattleItem item = ResourceMgr.PopItem(id);
					item.Type = id;
					item.m_Transform.SetParent(plat.m_Transform, false);
				}
			}
			_dicPlat.Add(_iPlatIndex, plat);
		}

		#region --Skill------------------------------------------------------------------
		public void SetMainSkill(int skillId)
		{
			BattleSkillBtn skill = ResourceMgr.PopSkill(skillId);
			skill.IsMainSkill = true;
			skill.m_Transform.SetParent(SkillBarTran);
			skill.SetToLargerScale();
			skill.SetPosImmediately(-4.7f);
		}

		private Queue<BattleSkillBtn> _queSkill = new Queue<BattleSkillBtn>();
		public void EnqueueSkill(int skillType)
		{
			if(_queSkill.Count > 4)
				DequeueSkill();

			BattleSkillBtn skill = ResourceMgr.PopSkill(skillType);
			skill.m_Transform.SetParent(SkillBarTran);
			skill.SetToOriScale();
			_queSkill.Enqueue(skill);
			ResetSkillBar();
		}

		public void DequeueSkill()
		{
			BattleSkillBtn skill = _queSkill.Dequeue();
			ResourceMgr.PushSkill(skill.Type, skill);
			ResetSkillBar();
		}

		private void ResetSkillBar()
		{
			int i = 0;
			foreach(BattleSkillBtn skill in _queSkill)
			{
				skill.SetPosImmediately(-2.8f + 1.6f * i++);
			}
		}

		//------
		public void ChangePlatType(int count, int type)
		{
			float curHeroY = Hero.m_Transform.localPosition.y;
			for(int i = _iCurMinIndex; i < count; ++i)
			{
				if(_dicPlat.ContainsKey(i))
				{
					if(_dicPlat[i].Y < curHeroY)
						RemovePlat(_dicPlat[i]);
				}
			}
		}

		private Vector3 _vecScale = new Vector3(0.5f, 1f, 1f);
		public void ChangePlatScale(int count)
		{
			float curHeroY = Hero.m_Transform.localPosition.y;
			for(int i = _iCurMinIndex; i < count; ++i)
			{
				if(_dicPlat.ContainsKey(i))
				{
					if(_dicPlat[i].Y < curHeroY)
						_dicPlat[i].m_Transform.localScale = _vecScale;
				}
			}
		}

		public void ChangeHeroHp(int changeValue)
		{
			CurHP += changeValue;
		}
		#endregion
	}
}
