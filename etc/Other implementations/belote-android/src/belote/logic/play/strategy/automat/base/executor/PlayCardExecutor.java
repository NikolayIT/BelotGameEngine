/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.base.executor;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.base.PlayCardMethod;

/**
 * PlayCardExecutor abstract class. Provides the mechanism to be executed one by one PlayCardMethod methods stored in an collection and to return the first not
 * null result value returned from the iteration. Also provides the facility the user to write own code which is executed before the result to be returned.
 * @author Dimitar Karamanov
 */
public abstract class PlayCardExecutor implements PlayCardMethod {

    /**
     * Collections which holds the PlayCardMethod methods.
     */
    private final PlayCardMethodList list = new PlayCardMethodList();

    /**
     * Belote game internal object.
     */
    protected final Game game;

    /**
     * Constructor
     * @param game BelotGame instance.
     */
    public PlayCardExecutor(final Game game) {
        this.game = game;
    }

    /**
     * Returns player's card by executing one by one the collection's methods.
     * @param player for which the card is retrieved.
     * @return Card object instance or null.
     */
    public final Card getPlayerCard(final Player player) {
        Card result = null;

        if (fitPreCondition(player)) {
            for (final PlayCardMethodIterator iterator = list.iterator(); iterator.hasNext() && result == null;) {
                final PlayCardMethod playable = iterator.next();
                result = playable.getPlayerCard(player);
            }

            afterExecution(player, result);
        }
        return result;
    }

    /**
     * Handler method providing the user facility to check custom condition for methods executions.
     * @param player for which is called the executor
     * @return true to process method execution false to not.
     */
    protected boolean fitPreCondition(final Player player) {
        return true; // Override to check own condition for processing
    }

    /**
     * Handler method providing the user to write additional code which is executed after the getPlayerCard(Player).
     * @param player for which is called the executor
     * @param result the result of the method getPlayerCard(Player)
     */
    protected void afterExecution(final Player player, final Card result) {
        // Blank method which the user can implemented later.
    }

    /**
     * Adds method to the execution collection.
     * @param method which is added to collection.
     */
    protected void register(final PlayCardMethod method) {
        list.add(method);
    }
}
