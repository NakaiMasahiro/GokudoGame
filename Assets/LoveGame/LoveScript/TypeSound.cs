using System;
using UnityEngine;

// 文字が1文字ずつ出るときの効果音の設定(Talk と EndingManager で共通)
[Serializable]
public class TypeSound
{
    [Tooltip("音を鳴らすAudioSource(空なら鳴らさない)")]
    public AudioSource source;

    [Tooltip("文字が出るときの音")]
    public AudioClip clip;

    [Range(0f, 1f)] public float volume = 1f;

    [Min(1)]
    [Tooltip("何文字ごとに鳴らすか。1=毎文字 / 2=2文字に1回(速い文字送りだとうるさくなりにくい)")]
    public int everyChars = 2;

    [Tooltip("音の高さをランダムにずらす範囲(1=そのまま)。同じ音が続いても単調になりにくい")]
    public Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    // count: いま何文字目まで出たか(1から数える)。c: その文字
    public void Play(char c, int count)
    {
        if (source == null || clip == null) return;
        if ((count - 1) % Mathf.Max(1, everyChars) != 0) return;
        if (char.IsWhiteSpace(c) || "、。,.!?！？…・「」『』".IndexOf(c) >= 0) return;

        source.pitch = UnityEngine.Random.Range(pitchRange.x, pitchRange.y);
        source.PlayOneShot(clip, volume);
    }
}