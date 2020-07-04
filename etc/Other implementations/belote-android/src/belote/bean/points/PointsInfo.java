/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.points;

import java.io.Serializable;

/**
 * GamePointsInfo class.
 * @author Dimitar Karamanov
 */
public final class PointsInfo implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = -6993520653948056582L;

    /**
     * Current game points.
     */
    private int cardPoints = 0;

    /**
     * Current game last hand.
     */
    private int lastHand = 0;

    /**
     * Current game announce points.
     */
    private int announcePoints = 0;

    /**
     * Current game couples points.
     */
    private int couplesPoints = 0;

    /**
     * Current game total points.
     */
    private int totalPoints = 0;

    /**
     * Current game total trick points.
     */
    private int totalTrickPoints = 0;

    /**
     * Current game capot points.
     */
    private int capotPoints = 0;

    /**
     * Constructor.
     */
    public PointsInfo() {
        super();
    }

    /**
     * Gets announce points.
     * @return announcePoints.
     */
    public int getAnnouncePoints() {
        return announcePoints;
    }

    /**
     * Sets announce points.
     * @param announcePoints new ones.
     */
    public void setAnnouncePoints(int announcePoints) {
        this.announcePoints = announcePoints;
    }

    /**
     * Gets capot points.
     * @return capotPoints.
     */
    public int getCapotPoints() {
        return capotPoints;
    }

    /**
     * Sets capot points.
     * @param capotPoints new ones.
     */
    public void setCapotPoints(int capotPoints) {
        this.capotPoints = capotPoints;
    }

    /**
     * Gets card points.
     * @return cardPoints.
     */
    public int getCardPoints() {
        return cardPoints;
    }

    /**
     * Sets card points.
     * @param cardPoints new ones.
     */
    public void setCardPoints(final int cardPoints) {
        this.cardPoints = cardPoints;
    }

    /**
     * Gets couples points.
     * @return couplesPoints.
     */
    public int getCouplesPoints() {
        return couplesPoints;
    }

    /**
     * Sets couples points.
     * @param couplesPoints new ones.
     */
    public void setCouplesPoints(final int couplesPoints) {
        this.couplesPoints = couplesPoints;
    }

    /**
     * Gets last hand points.
     * @return lastHand.
     */
    public int getLastHand() {
        return lastHand;
    }

    /**
     * Sets last hands points.
     * @param lastHand new ones.
     */
    public void setLastHand(final int lastHand) {
        this.lastHand = lastHand;
    }

    /**
     * Gets total points.
     * @return totalPoints.
     */
    public int getTotalPoints() {
        return totalPoints;
    }

    /**
     * Sets total points.
     * @param totalPoints new ones.
     */
    public void setTotalPoints(final int totalPoints) {
        this.totalPoints = totalPoints;
    }

    /**
     * Gets total trick points.
     * @return totalTrickPoints.
     */
    public int getTotalTrickPoints() {
        return totalTrickPoints;
    }

    /**
     * Sets total trick points.
     * @param totalTrickPoints new ones .
     */
    public void setTotalTrickPoints(final int totalTrickPoints) {
        this.totalTrickPoints = totalTrickPoints;
    }

    /**
     * Resets all points values.
     */
    public void resetAllPoints() {
        announcePoints = 0;
        cardPoints = 0;
        couplesPoints = 0;
        lastHand = 0;
        capotPoints = 0;
        totalPoints = 0;
        totalTrickPoints = 0;
    }
}
