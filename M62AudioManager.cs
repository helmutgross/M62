using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace M62Logic
{
	// Token: 0x02000005 RID: 5
	public class M62AudioManager : MonoBehaviour
	{
		// Token: 0x06000006 RID: 6 RVA: 0x00002290 File Offset: 0x00000490
		private void Start()
		{
			base.StartCoroutine(this.LoadAudio("compressor.wav", delegate(AudioClip clip)
			{
				M62AudioManager.compressorClip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("typhon.wav", delegate(AudioClip clip)
			{
				M62AudioManager.typhonClip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("svist.wav", delegate(AudioClip clip)
			{
				M62AudioManager.whistleClip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("oilpump.wav", delegate(AudioClip clip)
			{
				M62AudioManager.oilpumpClip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("395_click.wav", delegate(AudioClip clip)
			{
				M62AudioManager.click395Clip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("395_hiss.wav", delegate(AudioClip clip)
			{
				M62AudioManager.hiss395Clip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("395_charge.wav", delegate(AudioClip clip)
			{
				M62AudioManager.charge395Clip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("254.wav", delegate(AudioClip clip)
			{
				M62AudioManager.click254Clip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("blowdown.wav", delegate(AudioClip clip)
			{
				M62AudioManager.blowdownClip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("contactor_main.wav", delegate(AudioClip clip)
			{
				M62AudioManager.contactorMainClip = clip;
			}));
			base.StartCoroutine(this.LoadAudio("contactor_op.wav", delegate(AudioClip clip)
			{
				M62AudioManager.contactorOpClip = clip;
			}));
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000024B9 File Offset: 0x000006B9
		private IEnumerator LoadAudio(string filename, Action<AudioClip> onSuccess)
		{
			string path = "file:///" + Main.mod.Path + filename;
			UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.WAV);
			try
			{
				yield return uwr.SendWebRequest();
				bool flag = !string.IsNullOrEmpty(uwr.error);
				if (flag)
				{
					Main.mod.Logger.Log("[M62] [ОШИБКА] Не удалось загрузить " + filename + ": " + uwr.error);
				}
				else
				{
					onSuccess(DownloadHandlerAudioClip.GetContent(uwr));
				}
			}
			finally
			{
				if (uwr != null)
				{
					((IDisposable)uwr).Dispose();
				}
			}
		}

		// Token: 0x04000006 RID: 6
		public static AudioClip compressorClip;

		// Token: 0x04000007 RID: 7
		public static AudioClip typhonClip;

		// Token: 0x04000008 RID: 8
		public static AudioClip whistleClip;

		// Token: 0x04000009 RID: 9
		public static AudioClip oilpumpClip;

		// Token: 0x0400000A RID: 10
		public static AudioClip click395Clip;

		// Token: 0x0400000B RID: 11
		public static AudioClip hiss395Clip;

		// Token: 0x0400000C RID: 12
		public static AudioClip charge395Clip;

		// Token: 0x0400000D RID: 13
		public static AudioClip click254Clip;

		// Token: 0x0400000E RID: 14
		public static AudioClip blowdownClip;

		// Token: 0x0400000F RID: 15
		public static AudioClip contactorMainClip;

		// Token: 0x04000010 RID: 16
		public static AudioClip contactorOpClip;
	}
}
