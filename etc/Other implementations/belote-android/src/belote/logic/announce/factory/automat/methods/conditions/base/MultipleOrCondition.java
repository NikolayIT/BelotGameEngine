/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions.base;

import belote.bean.Player;

/**
 * MultipleOrCondition class. MultipleOrCondition is an complicated condition which is compound of several announce conditions (which can be single or
 * complicated too) and the result is the boolean OR of all of them.
 * @author Dimitar Karamanov
 */
public final class MultipleOrCondition implements AnnounceCondition {

    private final AnnounceConditionList announceConditionsList;

    public MultipleOrCondition() {
        announceConditionsList = new AnnounceConditionList();
    }

    /**
     * Constructor.
     * @param condition0 to be added to the list.
     */
    public MultipleOrCondition(final AnnounceCondition condition0) {
        announceConditionsList = new AnnounceConditionList();
        announceConditionsList.add(condition0);
    }

    /**
     * Constructor.
     * @param condition0 to be added to the list.
     * @param condition1 to be added to the list.
     */
    public MultipleOrCondition(final AnnounceCondition condition0, final AnnounceCondition condition1) {
        this(condition0);
        announceConditionsList.add(condition1);
    }

    /**
     * Constructor.
     * @param condition0 to be added to the list.
     * @param condition1 to be added to the list.
     * @param condition2 to be added to the list.
     */
    public MultipleOrCondition(final AnnounceCondition condition0, final AnnounceCondition condition1, final AnnounceCondition condition2) {
        this(condition0, condition1);
        announceConditionsList.add(condition2);
    }

    /**
     * Constructor.
     * @param condition0 to be added to the list.
     * @param condition1 to be added to the list.
     * @param condition2 to be added to the list.
     * @param condition3 to be added to the list.
     */
    public MultipleOrCondition(final AnnounceCondition condition0, final AnnounceCondition condition1, final AnnounceCondition condition2,
            final AnnounceCondition condition3) {
        this(condition0, condition1, condition2);
        announceConditionsList.add(condition3);
    }

    /**
     * Adds an announce condition to the list.
     * @param announceCondition to be added.
     */
    public final void addAnnounceCondition(final AnnounceCondition announceCondition) {
        announceConditionsList.add(announceCondition);
    }

    /**
     * The method which returns the result of condition. (Multiple OR)
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        for (final AnnounceConditionIterator iterator = announceConditionsList.iterator(); iterator.hasNext();) {
            if (iterator.next().process(player)) {
                return true;
            }
        }
        return false;
    }
}
