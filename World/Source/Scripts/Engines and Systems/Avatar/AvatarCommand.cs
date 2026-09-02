using Server.Commands;
using Server.Commands.Generic;
using Server.Gumps;
using Server.Misc;
using Server.Mobiles;
using Server.Utilities;
using System;

namespace Server.Engines.Avatar
{
	public class AvatarCommand
	{
		public static void Initialize()
		{
			CommandSystem.Register("avatar-enable", AccessLevel.Player, new CommandEventHandler(EnableAvatarCommand));
			CommandSystem.Register("avatar-draft-enable", AccessLevel.Player, new CommandEventHandler(EnableDraftCommand));
			CommandSystem.Register("avatar-shop", AccessLevel.Player, new CommandEventHandler(OpenAvatarShopCommand));
			CommandSystem.Register("avatar-migrate--game-time", AccessLevel.Administrator, new CommandEventHandler(OnMigrateGameTime));
			TargetCommands.Register(new DraftBanUnbanSkillCommand(true));
			TargetCommands.Register(new DraftBanUnbanSkillCommand(false));
		}

		[Usage("avatar-enable")]
		[Description("Enables the Avatar status for the Player.")]
		public static void EnableAvatarCommand(CommandEventArgs e)
		{
			var from = (PlayerMobile)e.Mobile;
			if (!AvatarShopGump.InGypsyEncampment(from))
			{
				from.SendMessage("You must be in the Gypsy encampment to become an Avatar.");
				return;
			}

			if (from.Avatar.Active)
			{
				from.SendMessage("You already have the Avatar status enabled.");
				return;
			}

			var confirmation = new ConfirmationGump(
				from,
				"Enable Avatar Status?",
				"Are you sure you wish to enable the Avatar status? This will reset your character and allow you to use the Avatar features.",
				() =>
				{
					from.SendMessage("Your character will be recreated and you will be disconnected shortly...");

					Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
					{
						var _ = AvatarEngine.Instance.GetOrCreateContext(from);
						var newCharacter = CharacterCreation.ResetCharacter(from, false, false);
						AvatarEngine.InitializePlayer(newCharacter);
						AvatarEngine.Instance.ApplyContext(newCharacter, newCharacter.Avatar);
					});
				}
			);
			from.SendGump(confirmation);
		}

		[Usage("avatar-draft-enable")]
		[Description("Enables the Avatar status and Draft mode for the Player.")]
		public static void EnableDraftCommand(CommandEventArgs e)
		{
			var from = (PlayerMobile)e.Mobile;
			if (!AvatarShopGump.InGypsyEncampment(from))
			{
				from.SendMessage("You must be in the Gypsy encampment to enable Draft mode.");
				return;
			}

			if (from.Avatar.Active && from.AccessLevel <= AccessLevel.Player)
			{
				from.SendMessage("You already have the Avatar status enabled.");
				return;
			}

			var confirmation = new ConfirmationGump(
				from,
				"Enable Avatar Draft Mode?",
				"Are you sure you wish to enable the Avatar Draft mode? This will reset your character and allow you to use the Avatar Draft features.",
				() =>
				{
					from.SendMessage("Your character will be recreated and you will be disconnected shortly...");

					Timer.DelayCall(TimeSpan.FromSeconds(1), () =>
					{
						var _ = AvatarEngine.Instance.GetOrCreateContext(from);
						var newCharacter = CharacterCreation.ResetCharacter(from, false, false);
						AvatarEngine.InitializePlayer(newCharacter);
						AvatarEngine.Instance.ApplyContext(newCharacter, newCharacter.Avatar);

						newCharacter.Avatar.SetDraftModeEnabled(newCharacter, true);
					});
				}
			);
			from.SendGump(confirmation);
		}

		[Description("Adds all the game time from the death contexts to the avatar's lifetime game time.")]
		public static void OnMigrateGameTime(CommandEventArgs e)
		{
			var avatars = WorldUtilities.ForEachMobile<PlayerMobile>(pm => pm != null && !pm.Deleted && pm.Avatar.Active);
			foreach (var avatar in avatars)
			{
				foreach (var deathContext in DeathContext.GetAllDeathContexts(avatar))
				{
					if (0 < deathContext.Version) continue;

					avatar.Avatar.LifetimeGameTime = avatar.Avatar.LifetimeGameTime.Add(deathContext.GameTime);
				}
			}

			e.Mobile.SendMessage("Deaths loaded successfully.");
		}

		[Usage("avatar-shop")]
		[Description("Opens the Avatar Shop for the Player.")]
		public static void OpenAvatarShopCommand(CommandEventArgs e)
		{
			var from = (PlayerMobile)e.Mobile;
			if (!from.Avatar.Active)
			{
				from.SendMessage("You do not have the Avatar status enabled.");
				return;
			}

			from.SendGump(new AvatarShopGump(from));
		}

		private class DraftBanUnbanSkillCommand : BaseCommand
		{
			private readonly bool _isBan;

			public DraftBanUnbanSkillCommand(bool isBan)
			{
				_isBan = isBan;

				var command = isBan ? "avatar-draft-ban" : "avatar-draft-unban";
				AccessLevel = AccessLevel.GameMaster;
				Supports = CommandSupport.AllMobiles;
				Commands = new string[] { command };
				ObjectTypes = ObjectTypes.Mobiles;
				Usage = string.Format("{0} <skill>", command);
				Description = isBan
					? "Prevents the specified skill from showing up as an option in the Avatar Draft mode."
					: "Removes the ban on the specified skill to allow it to show up as an option in the Avatar Draft mode.";
			}

			public override void Execute(CommandEventArgs arg, object obj)
			{
				if (arg.Length != 1)
				{
					arg.Mobile.SendMessage(Usage);
					return;
				}

				SkillName skill;
				try
				{
					skill = (SkillName)Enum.Parse(typeof(SkillName), arg.GetString(0), true);
				}
				catch
				{
					arg.Mobile.SendMessage("Invalid skill name.");
					return;
				}

				var from = (PlayerMobile)arg.Mobile;
				var pm = obj as PlayerMobile;
				if (pm == null)
				{
					LogFailure("That is not a player.");
					return;
				}

				if (!pm.Avatar.Active)
				{
					LogFailure("That is not an Avatar.");
					return;
				}

				if (!pm.Avatar.DraftModeEnabled)
				{
					LogFailure("That character is not in Draft mode.");
					return;
				}

				if (_isBan)
				{
					pm.Avatar.AddDraftBannedSkill(skill);
					from.SendMessage("'{0}' will no longer see '{1}' when drafting skills.", pm.Name, skill);
				}
				else
				{
					pm.Avatar.RemoveDraftBannedSkill(skill);
					from.SendMessage("'{0}' may now see '{1}' when drafting skills.", pm.Name, skill);
				}
			}
		}
	}
}