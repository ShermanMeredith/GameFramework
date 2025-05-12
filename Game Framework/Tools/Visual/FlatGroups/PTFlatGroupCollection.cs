using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Text;
namespace PlayTable
{
    [Serializable]
    public class PTFlatGroupCollection
    {
        public List<PTFlatGroupElement> content = new List<PTFlatGroupElement>();
        public bool isGroup { get { return isGroupIfSingle || Count > 1; } }
        public int Count { get { return content.Count; } }
        public PTFlatGroupBackground myBackground;

        public bool isGroupIfSingle = false;

        public PTFlatGroupCollection(bool isGroupIfSingle)
        {
            this.isGroupIfSingle = isGroupIfSingle;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PTFlatGroupCollection: {");
            foreach (PTFlatGroupElement element in content)
            {
                sb.Append(element.ToString()).Append(", ");
            }
            sb.Append("}; isGroup=").Append(isGroup);
            return sb.ToString();
        }

        public PTFlatGroupElement Get(int index)
        {
            if (index < 0 || index >= Count)
            {
                return null;
            }
            else
            {
                return content[index];
            }
        }

        public void Add(PTFlatGroupElement element)
        {
            if (element && !Contains(element))
            {
                content.Add(element);
            }
        }

        public void Remove(PTFlatGroupElement element)
        {
            if (element && Contains(element))
            {
                content.Remove(element);
            }
        }

        public bool Contains(PTFlatGroupElement element)
        {
            return content.Find(x => x == element) != null;
        }

        public void UpdateBackground(PTFlatGroups groups)
        {
            if (content.Count < 1)
            {
                if (myBackground)
                {
                    UnityEngine.Object.Destroy(myBackground.gameObject);
                }
                return;
            }

            if (isGroup)
            {
                if (!myBackground)
                {
                    //instantiate a background instance
                    myBackground = UnityEngine.Object.Instantiate(groups.backgroundPrefab, groups.transform);
                    myBackground.transform.position = content[0].transform.position + groups.positionBackgroudSpawn;
                }
                myBackground.UpdateBackground(new KeyValuePair<int, PTFlatGroupCollection>(groups.IndexOf(this), this));
            }
            else
            {
                if (myBackground)
                {
                    UnityEngine.Object.Destroy(myBackground.gameObject);
                }
            }
        }

        public int IndexOf(PTFlatGroupElement element)
        {
            return content.IndexOf(element);
        }

        public void Insert(int index, PTFlatGroupElement element)
        {
            content.Insert(index, element);
        }
    }
}