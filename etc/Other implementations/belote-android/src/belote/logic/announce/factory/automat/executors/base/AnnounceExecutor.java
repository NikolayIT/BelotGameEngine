/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.executors.base;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.type.AnnounceType;
import belote.logic.announce.factory.automat.base.AnnounceMethod;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceConditionIterator;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceConditionList;

/**
 * AnnounceExecutor abstract class. Provides the mechanism to be executed one by one PlayCardMethod methods stored in an collection and to return the first not
 * null result value returned from the iteration. Also provides the facility the user to write own code which is executed before the result to be returned.
 * @author Dimitar Karamanov
 */
public abstract class AnnounceExecutor implements AnnounceMethod {

    /**
     * Belote game internal object.
     */
    protected final Game game;

    /**
     * List with preCondition
     */
    private final AnnounceConditionList preConditionsList = new AnnounceConditionList();

    /**
     * Collections which holds the PlayCardMethod methods.
     */
    private final AnnounceMethodList list = new AnnounceMethodList();

    /**
     * Constructor
     * @param game BelotGame instance.
     */
    public AnnounceExecutor(final Game game) {
        this.game = game;
    }

    /**
     * Returns player's announce by executing one by one the collection's methods.
     * @param player for which the card is retrieved.
     * @return Announce object instance or null.
     */
    private Announce getAnnounceMethod(final Player player) {
        Announce result = null;

        if (fitPreCondition(player)) {
            for (final AnnounceMethodIterator iterator = list.iterator(); iterator.hasNext() && result == null;) {
                final AnnounceMethod announceMethod = iterator.next();
                result = announceMethod.getAnnounce(player);

                if (result != null && !checkAnnounce(result)) {
                    result = null;
                }
            }
        }

        return afterExecution(player, result);
    }

    /**
     * Returns true if the provided announce can be made false otherwise.
     * @param announce provided announce.
     * @return boolean true if the provided announce can be made false otherwise.
     */
    private boolean checkAnnounce(final Announce announce) {
        Announce contractAnnounce = game.getAnnounceList().getContractAnnounce();
        if (contractAnnounce != null) {
            if (contractAnnounce.getAnnounceSuit().compareTo(announce.getAnnounceSuit()) < 0) {
                return announce.getType().equals(AnnounceType.Normal);
            }

            if (contractAnnounce.getAnnounceSuit().equals(announce.getAnnounceSuit())) {
                if (!contractAnnounce.getPlayer().isSameTeam(announce.getPlayer())) {
                    final boolean saidDoubleAfterNormal = contractAnnounce.getType().equals(AnnounceType.Normal)
                            && announce.getType().equals(AnnounceType.Double);
                    final boolean saidRedoubleAfterDouble = contractAnnounce.getType().equals(AnnounceType.Double)
                            && announce.getType().equals(AnnounceType.Redouble);
                    return saidDoubleAfterNormal || saidRedoubleAfterDouble;
                }
            }
            return false;
        }
        return true;
    }

    /**
     * Adds precondition to the internal container.
     * @param announceCondition which to be added.
     */
    protected final void addPreCondition(final AnnounceCondition announceCondition) {
        preConditionsList.add(announceCondition);
    }

    /**
     * Handler method providing the user facility to check custom condition for methods executions.
     * @param player for which is called the executor
     * @return true to process method execution false to not.
     */
    private boolean fitPreCondition(final Player player) {
        for (final AnnounceConditionIterator iterator = preConditionsList.iterator(); iterator.hasNext();) {
            if (!iterator.next().process(player)) {
                return false;
            }
        }
        return true;
    }

    /**
     * Handler method providing the user to write additional code which is executed after the getAnnounce(Player).
     * @param player for which is called the executor
     * @param announce the result of the method getAnnounce(Player)
     * @return Announce - the same or transformed one.
     */
    protected Announce afterExecution(final Player player, final Announce announce) {
        return announce;
    }

    /**
     * Adds method to the execution collection.
     * @param method which is added to collection.
     */
    protected final void register(final AnnounceMethod method) {
        list.add(method);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Announce object instance or null.
     */
    public final Announce getAnnounce(Player player) {
        final Announce result = getAnnounceMethod(player);
        if (result != null) {
            // handle result in future
        }
        return result;
    }
}
