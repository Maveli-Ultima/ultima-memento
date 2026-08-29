using Server.Mobiles;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Server.Engines.Avatar
{
	public partial class RewardFactory
	{
		public static List<IReward> CreateAscensionRewards(PlayerMobile from, PlayerContext context)
		{
			var currentErudianBonus = context.GetRecordedSkillCap();
			int erudianCapCost;
			if (currentErudianBonus < 70) erudianCapCost = SecondOrderCost(100, context.RecordedSkillCapLevel + 1);
			else if (currentErudianBonus < 90) erudianCapCost = SecondOrderCost(200, context.RecordedSkillCapLevel + 1);
			else if (currentErudianBonus < 100) erudianCapCost = SecondOrderCost(400, context.RecordedSkillCapLevel + 1);
			else if (currentErudianBonus < 105) erudianCapCost = SecondOrderCost(800, context.RecordedSkillCapLevel + 1);
			else if (currentErudianBonus < 110) erudianCapCost = SecondOrderCost(1000, context.RecordedSkillCapLevel + 1);
			else if (currentErudianBonus < 115) erudianCapCost = SecondOrderCost(1200, context.RecordedSkillCapLevel + 1);
			else if (currentErudianBonus < 120) erudianCapCost = SecondOrderCost(2400, context.RecordedSkillCapLevel + 1);
			else erudianCapCost = SecondOrderCost(4800, context.RecordedSkillCapLevel + 1);

			var currentSkillCap = (Constants.SKILL_CAP_BASE / 10) + (context.SkillCapLevel * Constants.SKILL_CAP_PER_LEVEL);
			int skillCapCost;
			if (currentSkillCap < 400) skillCapCost = SecondOrderCost(200, 1);
			else if (currentSkillCap < 500) skillCapCost = SecondOrderCost(800, 1);
			else if (currentSkillCap < 600) skillCapCost = SecondOrderCost(1600, 1);
			else if (currentSkillCap < 700) skillCapCost = SecondOrderCost(3200, 1);
			else if (currentSkillCap < 800) skillCapCost = SecondOrderCost(6400, 1);
			else if (currentSkillCap < 900) skillCapCost = SecondOrderCost(12800, 1);
			else if (currentSkillCap < 1000) skillCapCost = SecondOrderCost(51200, 1);
			else skillCapCost = SecondOrderCost(4000, 1);

			int statCapCost;
			if (context.StatCapLevel < 10) statCapCost = SecondOrderCost(200, 1);
			else if (context.StatCapLevel < 20) statCapCost = SecondOrderCost(600, 1);
			else if (context.StatCapLevel < 30) statCapCost = SecondOrderCost(1200, 1);
			else if (context.StatCapLevel < 40) statCapCost = SecondOrderCost(2400, 1);
			else if (context.StatCapLevel < 50) statCapCost = SecondOrderCost(4800, 1);
			else if (context.StatCapLevel < 60) statCapCost = SecondOrderCost(9600, 1);
			else if (context.StatCapLevel < 70) statCapCost = SecondOrderCost(19200, 1);
			else if (context.StatCapLevel < 80) statCapCost = SecondOrderCost(38400, 1);
			else if (context.StatCapLevel < 90) statCapCost = SecondOrderCost(76800, 1);
			else if (context.StatCapLevel < 100) statCapCost = SecondOrderCost(153600, 1);
			else if (context.StatCapLevel < 110) statCapCost = SecondOrderCost(307200, 1);
			else if (context.StatCapLevel < 120) statCapCost = SecondOrderCost(614400, 1);
			else if (context.StatCapLevel < 130) statCapCost = SecondOrderCost(1228800, 1);
			else if (context.StatCapLevel < 140) statCapCost = SecondOrderCost(2457600, 1);
			else statCapCost = SecondOrderCost(4915200, 1);

			int pointGainRateCost = SecondOrderCost(50, context.PointGainRateLevel + 1);
			int skillGainRateCost = ExponentialCost(2000, context.SkillGainRateLevel + 1);

			return new List<IReward>
			{
				!context.HasSafetyDepositBox
					? ActionReward.Create(
						context.HasSafetyDepositBox,
						ONE_HUNDRED_GOLD,
						AvatarShopGump.NO_ITEM_ID,
						"Persistent Storage Container",
						"A safety deposit box is placed in your bankbox. Items in this container will persist through death.",
						() => {
							var box = context.GetOrCreateSafetyDepositBox(from);
							context.SafetyDepositBoxLevel = Math.Max(1, context.SafetyDepositBoxLevel);
							from.SendMessage("A safety deposit box has been placed in your bank box.");
						}
					)
					: ActionReward.Create(
						Constants.SAFETY_DEPOSIT_BOX_MAX_LEVEL <= context.SafetyDepositBoxLevel,
						SecondOrderCost(ONE_HUNDRED_GOLD, context.SafetyDepositBoxLevel + 1),
						AvatarShopGump.NO_ITEM_ID,
						string.Format("Safety Deposit Box ({0} of {1})", context.SafetyDepositBoxLevel, Constants.SAFETY_DEPOSIT_BOX_MAX_LEVEL),
						string.Format("Increase storage capacity of your safety deposit box. Current capacity: {0}", context.SafetyDepositBoxLevel),
						() => {
							var box = context.GetOrCreateSafetyDepositBox(from);
							context.SafetyDepositBoxLevel += 1;
							box.MaxItems = context.SafetyDepositBoxLevel;
							from.SendMessage("Your safety deposit box can now hold {0} items.", context.SafetyDepositBoxLevel);
						}
					),
				ActionReward.Create(
					context.UnlockRecordSkillCaps,
					ONE_HUNDRED_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Erudian Teachings",
					"Reinforce your mind. Higher learning will become second nature.",
					() => {
						context.UnlockRecordSkillCaps = true;
						context.ClearRewardCache(Categories.PrimaryBoosts);
						context.ClearRewardCache(Categories.SecondaryBoosts);
						from.SendMessage("Your increased skill caps are now permanently unlocked.");
					}
				),
				ActionReward.Create(
					context.UnlockPrimarySkillBoost,
					2 * ONE_HUNDRED_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Jack of No Trades",
					"Learn from the greatest masters. Unlock the ability to restore Primary skills.",
					() => {
						context.UnlockPrimarySkillBoost = true;
						context.ClearRewardCache(Categories.PrimaryBoosts);
						context.ClearRewardCache(Categories.SecondaryBoosts);
						from.SendMessage("Some of your Primary skills are now available in the Skill Archive.");
					}
				).WithPrereq(
					context.UnlockRecordSkillCaps,
					"Requires Erudian Knowledge to be unlocked."
				),
				ActionReward.Create(
					context.UnlockSecondarySkillBoost,
					2 * ONE_HUNDRED_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Artisan's Mastery",
					"Master the crafts. Unlock the ability to restore Secondary skills.",
					() => {
						context.UnlockSecondarySkillBoost = true;
						context.ClearRewardCache(Categories.PrimaryBoosts);
						context.ClearRewardCache(Categories.SecondaryBoosts);
						from.SendMessage("Some of your Secondary skills are now available in the Skill Archive.");
					}
				).WithPrereq(
					context.UnlockRecordSkillCaps,
					"Requires Erudian Knowledge to be unlocked."
				),
				ActionReward.Create(
					context.UnlockRecordRecipes,
					ONE_THOUSAND_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Crafter Lineage",
					"Record recipes that you have learned.",
					() => {
						context.UnlockRecordRecipes = true;
						from.SendMessage("Your recipes are now permanently unlocked.");
					}
				),
				ActionReward.Create(
					context.UnlockRecordDiscovered,
					5 * ONE_THOUSAND_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"World Class Cartographer",
					"Discover the world and its wonders. Permanently record your travels to every land.",
					() => {
						context.UnlockRecordDiscovered = true;
						from.SendMessage("Your facet discoveries are now permanently recorded.");
					}
				),
				ActionReward.Create(
					Constants.BOAT_SPEED_MAX_LEVEL <= context.BoatSpeedLevel,
					ExponentialCost(5 * ONE_THOUSAND_GOLD, context.BoatSpeedLevel + 1),
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Fast Seaman ({0} of {1})", context.BoatSpeedLevel, Constants.BOAT_SPEED_MAX_LEVEL),
					"Increase your sailing speed.",
					() => {
						context.BoatSpeedLevel += 1;
						from.SendMessage("Your sailing speed has been increased.");
					}
				),
				ActionReward.Create(
					context.UnlockTemptations,
					10 * ONE_THOUSAND_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Power Overwhelming",
					"Answer the seductive call of power. Gain strength through temptation and desire.",
					() => {
						context.UnlockTemptations = true;
						from.SendMessage("You have unlocked the ability to use Temptations.");
					}
				),
				ActionReward.Create(
					context.UnlockSavageRace,
					25 * ONE_THOUSAND_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Primal Awakening",
					"Return to your untamed roots. Live life as a savage and embrace your barbaric heritage.",
					() => {
						context.UnlockSavageRace = true;
						from.SendMessage("You have unlocked a new tarot card for Humans.");
					}
				).WithPrereq(
					context.UnlockRecordDiscovered,
					"Requires World Class Cartographer to be unlocked."
				),
				ActionReward.Create(
					context.UnlockMonsterRaces,
					50 * ONE_THOUSAND_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Bestial Transformation",
					"Embrace your monstrous nature. Live among the supernatural and the beastly races of Ultima.",
					() => {
						context.UnlockMonsterRaces = true;
						from.SendMessage("You have unlocked the option to select a non-human race.");
					}
				),
				ActionReward.Create(
					context.UnlockFugitiveMode,
					150 * ONE_THOUSAND_GOLD,
					AvatarShopGump.NO_ITEM_ID,
					"Outlaw's Mark",
					"Bear the mark of the hunted. Strengthen your core and live as an exile.",
					() => {
						context.UnlockFugitiveMode = true;
						from.SendMessage("You have unlocked a new tarot card for Monsters and Humans.");
					}
				),

				ActionReward.Create(
					Constants.IMPROVED_TEMPLATE_MAX_COUNT <= context.ImprovedTemplateCount,
					ONE_HUNDRED_GOLD * (context.ImprovedTemplateCount + 1),
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Blessed Beginnings ({0} of {1})", context.ImprovedTemplateCount, Constants.IMPROVED_TEMPLATE_MAX_COUNT),
					string.Format("Awaken to your true potential. Ancestral relatives may enhance your template choices."),
					() => {
						context.ImprovedTemplateCount += 1;
						from.SendMessage("Your templates may now spawn as (Improved).");
					}
				),

				// Custom Templates
				ActionReward.Create(
					context.UnlockTemplateJester,
					10 * ONE_THOUSAND_GOLD,
					ITEM_ID_JESTER,
					"The Jester",
					"Unlock the ability to select the Jester template.",
					() => context.UnlockTemplateJester = true
				).WithPrereq(
					context.CanUnlockTemplateJester,
					"Requires Begging and Psychology to be at least 30."
				),
				ActionReward.Create(
					context.UnlockTemplateMystic,
					10 * ONE_THOUSAND_GOLD,
					ITEM_ID_MYSTIC,
					"The Mystic",
					"Unlock the ability to select the Mystic template.",
					() => context.UnlockTemplateMystic = true
				).WithPrereq(
					context.CanUnlockTemplateMystic,
					"Requires Focus and Meditation to be at least 100."
				),
				ActionReward.Create(
					context.UnlockTemplateShinobi,
					10 * ONE_THOUSAND_GOLD,
					ITEM_ID_SHINOB,
					"The Shinobi",
					"Unlock the ability to select the Shinobi template.",
					() => context.UnlockTemplateShinobi = true
				).WithPrereq(
					context.CanUnlockTemplateShinobi,
					"Requires Ninjitsu to be at least 50."
				),
				ActionReward.Create(
					context.UnlockTemplateDeathKnight,
					10 * ONE_THOUSAND_GOLD,
					ITEM_ID_DEATH_KNIGHT,
					"The Death Knight",
					"Unlock the ability to select the Death Knight template.",
					() => context.UnlockTemplateDeathKnight = true
				).WithPrereq(
					context.CanUnlockTemplateDeathKnight,
					"Requires Knightship to be at least 50."
				),
				ActionReward.Create(
					context.UnlockTemplateHolyMan,
					10 * ONE_THOUSAND_GOLD,
					ITEM_ID_HOLY_MAN,
					"The Holy Man",
					"Unlock the ability to select the Holy Man template.",
					() => context.UnlockTemplateHolyMan = true
				).WithPrereq(
					context.CanUnlockTemplateHolyMan,
					"Requires Healing and Spiritualism to be at least 30."
				),

				// Limits
				ActionReward.Create(
					Constants.RECORDED_SKILL_CAP_MAX_LEVEL <= context.RecordedSkillCapLevel,
					erudianCapCost,
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Erudian Knowledge ({0} of {1})", context.RecordedSkillCapLevel, Constants.RECORDED_SKILL_CAP_MAX_LEVEL),
					string.Format("Increases the maximum of skill that your Skill Archive can provide by {0}. Current maximum: {1}", Constants.RECORDED_SKILL_CAP_INTERVAL, context.GetRecordedSkillCap()),
					() => {
						context.RecordedSkillCapLevel += 1;
						context.ClearRewardCache(Categories.PrimaryBoosts);
						context.ClearRewardCache(Categories.SecondaryBoosts);
					}
				).WithPrereq(
					context.UnlockPrimarySkillBoost || context.UnlockSecondarySkillBoost,
					"Requires Jack of No Trades or Artisan's Mastery to be unlocked."
				),
				ActionReward.Create(
					Constants.SKILL_CAP_MAX_LEVEL <= context.SkillCapLevel,
					skillCapCost,
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Skill Cap ({0} of {1})", context.SkillCapLevel, Constants.SKILL_CAP_MAX_LEVEL),
					string.Format("Increases the skill cap by {0}. Current bonus: {1}", Constants.SKILL_CAP_PER_LEVEL, Constants.SKILL_CAP_PER_LEVEL * context.SkillCapLevel),
					() => context.SkillCapLevel += 1
				),
				ActionReward.Create(
					Constants.STAT_CAP_MAX_LEVEL <= context.StatCapLevel,
					statCapCost,
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Stat Cap ({0} of {1})", context.StatCapLevel, Constants.STAT_CAP_MAX_LEVEL),
					string.Format("Increases the stat cap by {0}. Current bonus: {1}", Constants.STAT_CAP_PER_LEVEL, Constants.STAT_CAP_PER_LEVEL * context.StatCapLevel),
					() => context.StatCapLevel += 1
				),

				// Rates
				ActionReward.Create(
					Constants.POINT_GAIN_RATE_MAX_LEVEL <= context.PointGainRateLevel,
					pointGainRateCost,
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Coins Gain Rate ({0} of {1})", context.PointGainRateLevel, Constants.POINT_GAIN_RATE_MAX_LEVEL),
					string.Format("Increases the coins gain rate by {0}%. Current bonus: {1}%", Constants.POINT_GAIN_RATE_PER_LEVEL, Constants.POINT_GAIN_RATE_PER_LEVEL * context.PointGainRateLevel),
					() => context.PointGainRateLevel += 1
				),
				ActionReward.Create(
					Constants.SKILL_GAIN_RATE_MAX_LEVEL <= context.SkillGainRateLevel,
					skillGainRateCost,
					AvatarShopGump.NO_ITEM_ID,
					string.Format("Skill Gain Rate ({0} of {1})", context.SkillGainRateLevel, Constants.SKILL_GAIN_RATE_MAX_LEVEL),
					string.Format("Increases the skill gain rate by {0}%. Current bonus: {1}%", Constants.SKILL_GAIN_RATE_PER_LEVEL, Constants.SKILL_GAIN_RATE_PER_LEVEL * context.SkillGainRateLevel),
					() => context.SkillGainRateLevel += 1
				),
			}.Where(r => r != null).ToList();
		}
	}
}


