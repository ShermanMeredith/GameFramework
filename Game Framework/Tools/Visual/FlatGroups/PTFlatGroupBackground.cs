using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PlayTable
{
    [RequireComponent(typeof(PTTransform))]
    public class PTFlatGroupBackground : MonoBehaviour
    {
        public Sprite[] oneToTwo;
        public Sprite spiteStretch;
        public Image imageSwapping;
        public Image imageStretch;

        private PTFlatGroups groups = null;
        public KeyValuePair<int, PTFlatGroupCollection> Collection { get; private set; }
        public int SiblingIndexHover
        {
            get
            {
                try { return groups.GetSiblingIndexHover(transform.position); }
                catch { return -1; }
            }
        }
        KeyValuePair<int, PTFlatGroupCollection> CollectionHover
        {
            get
            {
                try { return groups.FindCollectionWithIndexBy(SiblingIndexHover); }
                catch { return new KeyValuePair<int, PTFlatGroupCollection>(-1, null); }
            }
        }
        public int SiblingIndexExpected
        {
            get
            {
                try
                {
                    return groups.GetSiblingIndexExpected(Collection.Value.content[0]);
                }
                catch { return -1; }
            }
        }
        KeyValuePair<int, PTFlatGroupCollection> CollectionExpectedToSwap
        {
            get
            {
                try
                {
                    int siblingDifference = SiblingIndexHover - groups.GetSiblingIndex(Collection.Value.content[0]);
                    int indexNextCollection = siblingDifference == 0 ?
                        Collection.Key : Collection.Key + siblingDifference / Mathf.Abs(siblingDifference);

                    PTFlatGroupCollection nextCollection = groups.Collections[indexNextCollection];

                    if (nextCollection == null || Mathf.Abs(siblingDifference) >= nextCollection.Count)
                    {
                        return new KeyValuePair<int, PTFlatGroupCollection>(indexNextCollection, nextCollection);
                    }
                }
                catch { }
                return Collection;
            }
        }
        private int backgroundSize = 0;

        private void Awake()
        {
            groups = GetComponentInParent<PTFlatGroups>();

            PTGlobalInput_new.OnDragBegin += (PTTouch touch) =>
            {
                if (IsDraggingTab(touch))
                {
                    touch.AddFollower(GetComponent<Collider>(), GetComponent<PTLocalInput_new>().dragWorldPositionOffset);

                    foreach (PTFlatGroupElement element in Collection.Value.content)
                    {
                        touch.AddFollower(element.GetComponent<Collider>(), GetComponent<PTLocalInput_new>().dragWorldPositionOffset);
                    }
                }
            };
            PTGlobalInput_new.OnDrag += (PTTouch touch) =>
            {
                if (IsDraggingTab(touch))
                {
                    groups.SwapCollections(Collection.Key, CollectionExpectedToSwap.Key);
                }
            };
        }
        private IEnumerator SpriteSwap(bool toBeLarger)
        {
            imageSwapping.enabled = true;
            imageStretch.enabled = false;

            for (int i = 0; i < oneToTwo.Length; i++)
            {
                yield return new WaitForSeconds(groups.timerAnimation / oneToTwo.Length);
                int spriteIndex = toBeLarger ? i : oneToTwo.Length - 1 - i;
                imageSwapping.sprite = oneToTwo[spriteIndex];
            }
        }
        private IEnumerator Stretch(int size)
        {
            imageSwapping.enabled = false;
            imageStretch.enabled = true;
            imageStretch.sprite = spiteStretch;
            int targetSize = size > 1 ? size : 1;
            Vector2 targetSizeDelta = groups.sizeDeltaTwo + new Vector2(groups.widthDelta * (targetSize - 2), 0);
            yield return imageStretch.transform.SetSizeDeltaCoroutine(targetSizeDelta, groups.timerAnimation);

        }
        private IEnumerator SetAppearance(bool b)
        {
            imageSwapping.enabled = b;
            imageStretch.enabled = false;
            imageSwapping.sprite = oneToTwo[0];
            imageStretch.sprite = spiteStretch;
            yield return null;
        }

        public void UpdateBackground(KeyValuePair<int, PTFlatGroupCollection> collection)
        {
            Collection = collection;
            int size = Collection.Value == null ? 0 : Collection.Value.Count;
            if (size > 0)
            {
                List<PTTouch> touchesDraggingThis = PTGlobalInput_new.FindTouchesDragging(GetComponent<Collider>());
                if (touchesDraggingThis == null || touchesDraggingThis.Count == 0)
                {
                    transform.SetLocalPosition(groups.GetComponent<PTLayoutZone_new>().TargetLocalPositionOf(groups.GetSiblingIndex(Collection.Value.content[0])), groups.timerAnimation);
                }
            }
            switch (size)
            {
                case 0:
                    StartCoroutine(SetAppearance(false));
                    break;
                case 1:
                    if (backgroundSize == 0 || backgroundSize == 1)
                    {
                        StartCoroutine(SetAppearance(true));
                    }
                    else
                    {
                        StartCoroutine(SpriteSwap(false));
                    }
                    break;
                case 2:
                    if (backgroundSize == 1)
                    {
                        StartCoroutine(SpriteSwap(true));
                    }
                    else
                    {
                        StartCoroutine(Stretch(size));
                    }
                    break;
                default:
                    StartCoroutine(Stretch(size));
                    break;
            }
            backgroundSize = size;
        }
        public bool IsDraggingTab(PTTouch touch)
        {
            return this != null && touch != null
                && (
                imageStretch && touch.hitsBegin.ContainsKey(imageStretch.GetComponent<Collider>())
                || imageSwapping && touch.hitsBegin.ContainsKey(imageSwapping.GetComponent<Collider>())
                );
        }


    }
}