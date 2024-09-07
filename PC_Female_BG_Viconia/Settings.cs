using UnityModManagerNet;

namespace PC_Female_BG_Viconia {
	public class Settings : UnityModManager.ModSettings {
		public float MoveCooldown = 10.0f;
		public float MoveChance = 0.1f;
		public override void Save(UnityModManager.ModEntry modEntry) {
			Save(this, modEntry);
		}
	}
}
