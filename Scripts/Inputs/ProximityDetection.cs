using System;
using System.Collections.Generic;
using Godot;

public class ProximityDetection
{
    #region FIELDS -------------------------------------------------------------
    protected Node2D? hovered = null;
    protected double secondsInHover;
    protected double timeSinceLastHoverChange = 0;
    protected bool hasTriggeredHoverStart = false;
    #endregion -----------------------------------------------------------------



    #region PUBLIC FIELDS ------------------------------------------------------
    [Export] public float HoverLimit = 50.0f;
    [Export] public float HoverSwitchThreshold = 15.0f;
    [Export] public double HoverChangeDelay = 0.1;
    [Export] public float HoverHysteresis = 10.0f;
    [Export] public double SecondsInOverDetectThreshold = 0.0;

    public Action<Node2D> HoverStartAction;
    public Action<Node2D> HoverStillAction;
    public Action<Node2D> HoverEndAction;
    #endregion -----------------------------------------------------------------



    #region PROPERTIES ---------------------------------------------------------
    public Node2D? Hovered => hovered;
    #endregion -----------------------------------------------------------------



    public void Update(double delta, Vector2 focus, IEnumerable<Node2D> nodesThatCanBeHovered)
    {
        timeSinceLastHoverChange += delta;

        Node2D closestNode = null;
        var closestDist = float.PositiveInfinity;

        foreach (var x in nodesThatCanBeHovered)
        {
            var dist = focus.DistanceTo(x.GlobalPosition);
            if (dist < closestDist)
            {
                closestNode = x;
                closestDist = dist;
            }
        }

        // If no node is within hover limit, clear hover
        if (closestDist > HoverLimit)
        {
            if (hovered != null && hasTriggeredHoverStart)
            {
                HoverEndAction?.Invoke(hovered);
            }
            secondsInHover = 0;
            timeSinceLastHoverChange = 0;
            hasTriggeredHoverStart = false;
            hovered = null;
            return;
        }

        // If nothing is currently hovered, start tracking the closest node
        if (hovered == null)
        {
            secondsInHover = 0;
            timeSinceLastHoverChange = 0;
            hasTriggeredHoverStart = false;
            hovered = closestNode;
            return;
        }

        // If the same node is still closest, continue hovering
        if (hovered == closestNode)
        {
            secondsInHover += delta;

            // Only trigger hover events after threshold is met
            if (secondsInHover >= SecondsInOverDetectThreshold)
            {
                if (!hasTriggeredHoverStart)
                {
                    hasTriggeredHoverStart = true;
                    HoverStartAction?.Invoke(hovered);
                }
                else
                {
                    HoverStillAction?.Invoke(hovered);
                }
            }
            return;
        }

        // Different node is closest - check if we should switch
        var currentHoveredDist = focus.DistanceTo(hovered.GlobalPosition);

        // Apply hysteresis - current hovered node gets a "stickiness" bonus
        var effectiveCurrentDist = currentHoveredDist - HoverHysteresis;

        // Only switch if:
        // 1. Enough time has passed since last change (prevents rapid switching)
        // 2. New node is significantly closer (accounting for hysteresis)
        var shouldSwitch = timeSinceLastHoverChange >= HoverChangeDelay &&
                          closestDist < effectiveCurrentDist - HoverSwitchThreshold;

        if (shouldSwitch)
        {
            // End hover on current node (only if it was actually triggered)
            if (hasTriggeredHoverStart)
            {
                HoverEndAction?.Invoke(hovered);
            }

            // Start tracking new node
            secondsInHover = 0;
            timeSinceLastHoverChange = 0;
            hasTriggeredHoverStart = false;
            hovered = closestNode;
        }
        else
        {
            // Continue with current hover
            secondsInHover += delta;

            // Only trigger hover events after threshold is met
            if (secondsInHover >= SecondsInOverDetectThreshold)
            {
                if (!hasTriggeredHoverStart)
                {
                    hasTriggeredHoverStart = true;
                    HoverStartAction?.Invoke(hovered);
                }
                else
                {
                    HoverStillAction?.Invoke(hovered);
                }
            }
        }
    }
}