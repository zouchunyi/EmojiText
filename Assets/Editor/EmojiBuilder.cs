/*

	插件说明：用于将原始散图序列帧表情图片生成Atlas

	所有表情需要放到Asset/Framework/Resource/Emoji/Input中
	命名格式为	表情名_序列帧.png 	其中单帧表情为	表情名.png

	输出的Atlas会放到对应的Output中
	其中包含两张图片，一张图片Atlas，一张数据Atlas。分别对应Emoji Shader中的_MainTex和_DataTex
	输出的数据文件为emoji.txt，同时也会自动拷贝到Asset/GameData中，用于生成LuaTable
	
	
	Author:zouchunyi
*/

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class EmojiBuilder  {

	private const string OutputPath = "Assets/Emoji/Output/";
	private const string InputPath = "/Emoji/Input/";

	private static readonly Vector2[] AtlasSize = new Vector2[]{
		new Vector2(32,32),
		new Vector2(64,64),
		new Vector2(128,128),
		new Vector2(256,256),
		new Vector2(512,512),
		new Vector2(1024,1024),
		new Vector2(2048,2048)
	};

	struct EmojiInfo
	{
		public string key;
		public string x;
		public string y;
		public string size;
	}
	private const int EmojiSize = 32;//表情尺寸

	[MenuItem("EmojiText/Build Emoji")]
	public static void BuildEmoji()
	{
		List<char> keylist = new List<char> ();
		for (int i = 48; i <= 57; i++) {
			keylist.Add (System.Convert.ToChar(i));//0-9
		}
		for (int i = 65; i <= 90; i++) {
			keylist.Add (System.Convert.ToChar(i));//A-Z
		}
		for (int i = 97; i <= 122; i++) {
			keylist.Add (System.Convert.ToChar(i));//a-z
		}

		//搜集全部表情图片并确定每个表情由多少帧组成
		Dictionary<string,int> sourceDic = new Dictionary<string,int> ();
		string[] files = Directory.GetFiles (Application.dataPath + InputPath,"*.png");
		for (int i = 0; i < files.Length; i++) {
			string[] strs = files [i].Split ('/');
			string[] strs2 = strs [strs.Length - 1].Split ('.');
			string filename = strs2 [0];

			string[] t = filename.Split('_');
			string id = t [0];
			if (sourceDic.ContainsKey(id)) {
				sourceDic[id]++;
			} else {
				sourceDic.Add (id, 1);
			}
		}
			
		//没有输出目录自动创建
		if (!Directory.Exists (OutputPath)) {
			Directory.CreateDirectory (OutputPath);
		}	

		Dictionary<string,EmojiInfo> emojiDic = new Dictionary<string, EmojiInfo> ();

		int totalFrames = 0;
		foreach (int value in sourceDic.Values) {
			totalFrames += value;
		}
		Vector2 texSize = ComputeAtlasSize (totalFrames);
		Texture2D newTex = new Texture2D ((int)texSize.x, (int)texSize.y, TextureFormat.ARGB32, false);
		Texture2D dataTex = new Texture2D ((int)texSize.x / EmojiSize, (int)texSize.y / EmojiSize, TextureFormat.ARGB32, false);
		int x = 0;
		int y = 0;
		int keyindex = 0;
		foreach (string key in sourceDic.Keys) {

			for (int index = 0; index < sourceDic[key]; index++) {
				
				string path = "Assets" + InputPath + key;
				if (sourceDic[key] == 1) {
					path += ".png";
				} else {
					path += "_" + (index + 1).ToString() + ".png";
				}

				Texture2D asset = AssetDatabase.LoadAssetAtPath<Texture2D> (path);
				Color[] colors = asset.GetPixels (0); 

				for (int i = 0; i < EmojiSize; i++) {
					for (int j = 0; j < EmojiSize; j++) {
						newTex.SetPixel (x + i, y + j, colors [i + j * EmojiSize]);
					}
				}

				string t = System.Convert.ToString (sourceDic [key] - 1, 2);
				float r = 0, g = 0, b = 0;
				if (t.Length >= 3) {
					r = t [2] == '1' ? 0.5f : 0;
					g = t [1] == '1' ? 0.5f : 0;
					b = t [0] == '1' ? 0.5f : 0;
				} else if (t.Length >= 2) {
					r = t [1] == '1' ? 0.5f : 0;
					g = t [0] == '1' ? 0.5f : 0;
				} else {
					r = t [0] == '1' ? 0.5f : 0;
				}

				dataTex.SetPixel (x / EmojiSize, y / EmojiSize, new Color (r, g, b, 1));

				if (! emojiDic.ContainsKey (key)) {
					EmojiInfo info;
					if (keyindex < keylist.Count)
					{
						info.key = "[" + char.ToString(keylist[keyindex]) + "]";
					}else
					{
						info.key = "[" + char.ToString(keylist[keyindex / keylist.Count]) + char.ToString(keylist[keyindex % keylist.Count]) + "]";
					}
					info.x = (x * 1.0f / texSize.x).ToString();
					info.y = (y * 1.0f / texSize.y).ToString();
					info.size = (EmojiSize * 1.0f / texSize.x).ToString ();

					emojiDic.Add (key, info);
					keyindex ++;
				}

				x += EmojiSize;
				if (x >= texSize.x) {
					x = 0;
					y += EmojiSize;
				}
			}
		}

		byte[] bytes1 = newTex.EncodeToPNG ();
		string outputfile1 = OutputPath + "emoji_tex.png";
		File.WriteAllBytes (outputfile1, bytes1);

		byte[] bytes2 = dataTex.EncodeToPNG ();
		string outputfile2 = OutputPath + "emoji_data.png";
		File.WriteAllBytes (outputfile2, bytes2);

		using (StreamWriter sw = new StreamWriter (OutputPath + "emoji.txt",false)) {
			sw.WriteLine ("Name\tKey\tFrames\tX\tY\tSize");
			foreach (string key in emojiDic.Keys) {
				sw.WriteLine ("{" + key + "}\t" + emojiDic[key].key + "\t" + sourceDic[key] + "\t" + emojiDic[key].x + "\t" + emojiDic[key].y + "\t" + emojiDic[key].size);
			}
			sw.Close ();
		}

		File.Copy (OutputPath + "emoji.txt","Assets/StreamingAssets/emoji.txt",true);

		EditorUtility.DisplayDialog ("Success", "Generate Emoji Successful!", "OK");
	}

	private static Vector2 ComputeAtlasSize(int count)
	{
		long total = count * EmojiSize * EmojiSize;
		for (int i = 0; i < AtlasSize.Length; i++) {
			if (total <= AtlasSize [i].x * AtlasSize [i].y) {
				return AtlasSize [i];
			}
		}
		return Vector2.zero;
	}
}
