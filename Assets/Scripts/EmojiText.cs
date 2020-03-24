using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class EmojiText : Text 
{
	private const float ICON_SCALE_OF_DOUBLE_SYMBOLE = 0.7f;
    public override float preferredWidth => cachedTextGeneratorForLayout.GetPreferredWidth(emojiText, GetGenerationSettings(rectTransform.rect.size)) / pixelsPerUnit;
	public override float preferredHeight => cachedTextGeneratorForLayout.GetPreferredHeight(emojiText, GetGenerationSettings(rectTransform.rect.size)) / pixelsPerUnit;

    private string emojiText => Regex.Replace(text, "\\[[a-z0-9A-Z]+\\]", "%%");
	private static Dictionary<string,EmojiInfo> m_EmojiIndexDict = null;

	struct EmojiInfo
	{
		public float x;
		public float y;
		public float size;
	}
		
	readonly UIVertex[] m_TempVerts = new UIVertex[4];
	
	protected override void OnPopulateMesh(VertexHelper toFill)
	{
		if (font == null)
        {
            return;
        }
        if (m_EmojiIndexDict == null)
        {
            m_EmojiIndexDict = new Dictionary<string, EmojiInfo>();

			//load emoji data, and you can overwrite this segment code base on your project.
			TextAsset emojiContent = Resources.Load<TextAsset> ("emoji");
			string[] lines = emojiContent.text.Split ('\n');
			for(int i = 1 ; i < lines.Length; i ++)
			{
				if (! string.IsNullOrEmpty (lines [i])) {
					string[] strs = lines [i].Split ('\t');
					EmojiInfo info;
					info.x = float.Parse (strs [3]);
					info.y = float.Parse (strs [4]);
					info.size = float.Parse (strs [5]);
                    m_EmojiIndexDict.Add (strs [1], info);
				}
			}
		}

		Dictionary<int,EmojiInfo> emojiDic = new Dictionary<int, EmojiInfo> ();
		
		if (supportRichText)
        {
			int nParcedCount = 0;
			//[1] [123] 替换成#的下标偏移量			
			int nOffset = 0;
			MatchCollection matches = Regex.Matches (text, "\\[[a-z0-9A-Z]+\\]");
			for (int i = 0; i < matches.Count; i++)
            {
				EmojiInfo info;
				if (m_EmojiIndexDict.TryGetValue (matches[i].Value, out info))
                {
                    emojiDic.Add(matches[i].Index - nOffset + nParcedCount, info);
                    nOffset += matches [i].Length - 1;
					nParcedCount++;
				}
			}			
		}

		// We don't care if we the font Texture changes while we are doing our Update.
		// The end result of cachedTextGenerator will be valid for this instance.
		// Otherwise we can get issues like Case 619238.
		m_DisableFontTextureRebuiltCallback = true;

		Vector2 extents = rectTransform.rect.size;

		var settings = GetGenerationSettings(extents);
		cachedTextGenerator.Populate(emojiText, settings);		

		Rect inputRect = rectTransform.rect;

		// get the text alignment anchor point for the text in local space
		Vector2 textAnchorPivot = GetTextAnchorPivot(alignment);
		Vector2 refPoint = Vector2.zero;
		refPoint.x = Mathf.Lerp(inputRect.xMin, inputRect.xMax, textAnchorPivot.x);
		refPoint.y = Mathf.Lerp(inputRect.yMin, inputRect.yMax, textAnchorPivot.y);

        // Apply the offset to the vertices
        IList<UIVertex> verts = cachedTextGenerator.verts;
		float unitsPerPixel = 1 / pixelsPerUnit;
		int vertCount = verts.Count;

        // We have no verts to process just return (case 1037923)
        if (vertCount <= 0)
        {
            toFill.Clear();
            return;
        }

        Vector2 roundingOffset = new Vector2(verts[0].position.x, verts[0].position.y) * unitsPerPixel;
        roundingOffset = PixelAdjustPoint(roundingOffset) - roundingOffset;
        toFill.Clear();
		if (roundingOffset != Vector2.zero)
		{
		    for (int i = 0; i < vertCount; ++i)
		    {
		        int tempVertsIndex = i & 3;
		        m_TempVerts[tempVertsIndex] = verts[i];
		        m_TempVerts[tempVertsIndex].position *= unitsPerPixel;
		        m_TempVerts[tempVertsIndex].position.x += roundingOffset.x;
		        m_TempVerts[tempVertsIndex].position.y += roundingOffset.y;
		        if (tempVertsIndex == 3)
                {
                    toFill.AddUIVertexQuad(m_TempVerts);
                }
            }
		}
		else
		{
			for (int i = 0; i < vertCount; ++i)
            {
				EmojiInfo info;
				int index = i / 4;
				if (emojiDic.TryGetValue (index, out info))
                {
                    //compute the distance of '[' and get the distance of emoji 
                    //计算2个%%的距离
                    float emojiSize = 2 * (verts[i + 1].position.x - verts[i].position.x) * ICON_SCALE_OF_DOUBLE_SYMBOLE;

                    float fCharHeight = verts[i + 1].position.y - verts[i + 2].position.y;
                    float fCharWidth = verts[i + 1].position.x - verts[i].position.x;

                    float fHeightOffsetHalf = (emojiSize - fCharHeight) * 0.5f;
                    float fStartOffset = emojiSize * (1 - ICON_SCALE_OF_DOUBLE_SYMBOLE);

                    m_TempVerts [3] = verts [i];//1
					m_TempVerts [2] = verts [i + 1];//2
					m_TempVerts [1] = verts [i + 2];//3
					m_TempVerts [0] = verts [i + 3];//4

                    m_TempVerts[0].position += new Vector3(fStartOffset, -fHeightOffsetHalf, 0);
                    m_TempVerts[1].position += new Vector3(fStartOffset - fCharWidth + emojiSize, -fHeightOffsetHalf, 0);
                    m_TempVerts[2].position += new Vector3(fStartOffset - fCharWidth + emojiSize, fHeightOffsetHalf, 0);
					m_TempVerts [3].position += new Vector3(fStartOffset, fHeightOffsetHalf, 0);
					
					m_TempVerts [0].position *= unitsPerPixel;
					m_TempVerts [1].position *= unitsPerPixel;
					m_TempVerts [2].position *= unitsPerPixel;
					m_TempVerts [3].position *= unitsPerPixel;
					
					float pixelOffset = emojiDic [index].size / 32 / 2;
					m_TempVerts [0].uv1 = new Vector2 (emojiDic [index].x + pixelOffset, emojiDic [index].y + pixelOffset);
					m_TempVerts [1].uv1 = new Vector2 (emojiDic [index].x - pixelOffset + emojiDic [index].size, emojiDic [index].y + pixelOffset);
					m_TempVerts [2].uv1 = new Vector2 (emojiDic [index].x - pixelOffset + emojiDic [index].size, emojiDic [index].y - pixelOffset + emojiDic [index].size);
					m_TempVerts [3].uv1 = new Vector2 (emojiDic [index].x + pixelOffset, emojiDic [index].y - pixelOffset + emojiDic [index].size);

					toFill.AddUIVertexQuad (m_TempVerts);

                    i += 4 * 2 - 1;
                }
                else
                {					
					int tempVertsIndex = i & 3;
					m_TempVerts [tempVertsIndex] = verts [i];
					m_TempVerts [tempVertsIndex].position *= unitsPerPixel;
					if (tempVertsIndex == 3)
                    {
                        toFill.AddUIVertexQuad(m_TempVerts);
                    }
                }
			}

		}
		m_DisableFontTextureRebuiltCallback = false;
	}
}
