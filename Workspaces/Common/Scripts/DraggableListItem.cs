﻿#if UNITY_EDITOR
using ListView;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.EditorVR.Handles;
using UnityEditor.Experimental.EditorVR.Utilities;
using UnityEngine;

namespace UnityEditor.Experimental.EditorVR.Workspaces
{
    class DraggableListItem<TData, TIndex> : ListViewItem<TData, TIndex>, IGetPreviewOrigin, IUsesViewerScale
        where TData : ListViewItemData<TIndex>
    {
        const float k_MagnetizeDuration = 0.5f;
        protected const float k_DragDeadzone = 0.025f;

        protected Transform m_DragObject;

        protected float m_DragLerp;

        readonly Dictionary<Transform, Vector3> m_DragStarts = new Dictionary<Transform, Vector3>();
        bool m_DirectGrab;

        protected virtual bool singleClickDrag { get { return true; } }

        public Action<TIndex, Transform, bool> setRowGrabbed { protected get; set; }
        public Func<Transform, ListViewItem<TData, TIndex>> getGrabbedRow { protected get; set; }

        protected virtual void OnDragStarted(BaseHandle handle, HandleEventData eventData)
        {
            if (singleClickDrag)
            {
                m_DragObject = handle.transform;
                m_DragLerp = 0;
                StartCoroutine(Magnetize());
            }
            else
            {
                // Cache eventData.direct because it is always true while dragging
                m_DirectGrab = eventData.direct;
                m_DragObject = null;
                m_DragStarts[eventData.rayOrigin] = eventData.rayOrigin.position;
            }
        }

        // Smoothly interpolate grabbed object into position, instead of "popping."
        protected virtual IEnumerator Magnetize()
        {
            var startTime = Time.realtimeSinceStartup;
            var currTime = 0f;
            while (currTime < k_MagnetizeDuration)
            {
                currTime = Time.realtimeSinceStartup - startTime;
                m_DragLerp = currTime / k_MagnetizeDuration;
                yield return null;
            }
            m_DragLerp = 1;
            OnMagnetizeEnded();
        }

        protected virtual void OnDragging(BaseHandle handle, HandleEventData eventData)
        {
            if (singleClickDrag)
            {
                if (m_DragObject)
                {
                    var previewOrigin = this.GetPreviewOriginForRayOrigin(eventData.rayOrigin);
                    MathUtilsExt.LerpTransform(m_DragObject, previewOrigin.position, previewOrigin.rotation, m_DragLerp);
                }
            }
            else
            {
                // Only allow direct grabbing
                if (!m_DirectGrab)
                    return;

                var rayOrigin = eventData.rayOrigin;
                var dragStart = m_DragStarts[rayOrigin];
                var dragVector = rayOrigin.position - dragStart;
                var distance = dragVector.magnitude;

                if (m_DragObject == null && distance > k_DragDeadzone * this.GetViewerScale())
                {
                    m_DragObject = handle.transform;
                    OnGrabDragStart(handle, eventData, dragStart);
                }

                if (m_DragObject)
                    OnGrabDragging(handle, eventData, dragStart);
            }
        }

        protected virtual void OnGrabDragStart(BaseHandle handle, HandleEventData eventData, Vector3 dragStart) {}

        protected virtual void OnGrabDragging(BaseHandle handle, HandleEventData eventData, Vector3 dragStart) {}

        protected virtual void OnDragEnded(BaseHandle handle, HandleEventData eventData)
        {
            m_DragObject = null;
        }

        protected virtual void OnMagnetizeEnded() {}
    }
}

#endif
