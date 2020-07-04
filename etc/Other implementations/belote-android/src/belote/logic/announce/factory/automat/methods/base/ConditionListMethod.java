/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.base;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.base.AnnounceMethod;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceConditionIterator;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceConditionList;
import belote.logic.announce.factory.automat.methods.conditions.base.MultipleAndCondition;

/**
 * ConditionListMethod class. Class which contains several Announce conditions and return announce if one of them fits the conditions.
 * @author Dimitar Karamanov
 */
public abstract class ConditionListMethod implements AnnounceMethod {

    /**
     * Belote game internal object.
     */
    protected final Game game;

    /**
     * List with AnnounceCondition
     */
    private final AnnounceConditionList announceConditionsList = new AnnounceConditionList();

    /**
     * List with preCondition
     */
    private final MultipleAndCondition preConditionsList = new MultipleAndCondition();

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public ConditionListMethod(final Game game) {
        this.game = game;
    }

    public final Announce getAnnounce(final Player player) {
        Announce result = null;
        if (preConditionsList.process(player)) {
            for (final AnnounceConditionIterator iterator = announceConditionsList.iterator(); iterator.hasNext() && result == null;) {
                if (iterator.next().process(player)) {
                    result = createAnnounce(player);
                }
            }
        }

        if (result != null) {
            // handle result in future
        }

        return result;
    }

    /**
     * Adds announce condition to the internal container.
     * @param announceCondition which to be added.
     */
    protected final void addAnnounceCondition(final AnnounceCondition announceCondition) {
        announceConditionsList.add(announceCondition);
    }

    /**
     * Adds precondition to the internal container.
     * @param announceCondition which to be added.
     */
    protected final void addPreCondition(final AnnounceCondition announceCondition) {
        preConditionsList.addAnnounceCondition(announceCondition);
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    abstract protected Announce createAnnounce(final Player player);
}
