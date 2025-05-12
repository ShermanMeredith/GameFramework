using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using PlayTable;

public class PTSmartPieceMarker : MonoBehaviour
{
    public enum MarkerSprite { pinkSquare, yellowSquare, greenSquare, pinkCircle, YellowCircle, GreenCircle };

    [SerializeField] private TMP_Text markerText1;
    [SerializeField] private TMP_Text markerText2;
    [SerializeField] private SpriteRenderer markerBg;
    [SerializeField] private SpriteRenderer markerTextbox1;
    [SerializeField] private SpriteRenderer markerTextbox2;
    [SerializeField] private AudioSource markerSound;

    [SerializeField] private Sprite pinkTextbox;
    [SerializeField] private Sprite yellowTextbox;
    [SerializeField] private Sprite greenTextbox;

    [SerializeField] private Sprite pinkMarker_square;
    [SerializeField] private Sprite yellowMarker_square;
    [SerializeField] private Sprite greenMarker_square;

    [SerializeField] private Sprite pinkMarker_circle;
    [SerializeField] private Sprite yellowMarker_circle;
    [SerializeField] private Sprite greenMarker_circle;

    private AudioClip cardAudio;
    private AudioClip dieAudio;
    private AudioClip figureAudio;

    private string spValue = "";
    private PTTouch touch;

    public void Init(ScannedSmartPiece sp)
    {
        if (sp.data != null && sp.data.type != null && sp.data.type != "")
        {
            switch ((SmartPieceType)System.Enum.Parse(typeof(SmartPieceType), sp.data.type))
            {
                case SmartPieceType.card:
                    SetMarker(MarkerSprite.greenSquare);
                    transform.localScale = Vector3.one * 2;
                    break;

                case SmartPieceType.die:
                    if(markerBg.sprite != greenMarker_square)
                    {
                        SetMarker(MarkerSprite.yellowSquare);
                    }
                    break;

                case SmartPieceType.figure:
                    SetMarker(MarkerSprite.GreenCircle);
                    transform.localScale = Vector3.one;
                    break;

                default:
                    SetMarker(MarkerSprite.pinkCircle);
                    break;
            }
        }
        else
        {
            SetMarker(MarkerSprite.pinkCircle);
        }
        spValue = PTSmartPieceManager.GetSpValue(sp);
        touch = sp.origin.touch;
    }

    public void SetMarker(MarkerSprite newType)
    {
        switch (newType)
        {
            case MarkerSprite.pinkSquare:
                markerBg.sprite = pinkMarker_square;
                markerTextbox1.sprite = pinkTextbox;
                markerTextbox2.sprite = pinkTextbox;
                markerText1.text = "Invalid";
                markerText2.text = "Invalid";
                break;
            case MarkerSprite.yellowSquare:
                markerBg.sprite = yellowMarker_square;
                markerTextbox1.sprite = yellowTextbox;
                markerTextbox2.sprite = yellowTextbox;
                markerText1.text = "Verifying";
                markerText2.text = "Verifying";
                break;
            case MarkerSprite.greenSquare:
                markerBg.sprite = greenMarker_square;
                markerTextbox1.sprite = greenTextbox;
                markerTextbox2.sprite = greenTextbox;
                markerText1.text = spValue;
                markerText2.text = spValue;
                break;
            case MarkerSprite.pinkCircle:
                markerBg.sprite = pinkMarker_circle;
                markerTextbox1.sprite = pinkTextbox;
                markerTextbox2.sprite = pinkTextbox;
                markerText1.text = "Invalid";
                markerText2.text = "Invalid";
                break;
            case MarkerSprite.YellowCircle:
                markerBg.sprite = yellowMarker_circle;
                markerTextbox1.sprite = yellowTextbox;
                markerTextbox2.sprite = yellowTextbox;
                markerText1.text = "Verifying";
                markerText2.text = "Verifying";
                break;
            case MarkerSprite.GreenCircle:
                markerBg.sprite = greenMarker_circle;
                markerTextbox1.sprite = greenTextbox;
                markerTextbox2.sprite = greenTextbox;
                markerText1.text = spValue;
                markerText2.text = spValue;
                break;
        }
    }
}
