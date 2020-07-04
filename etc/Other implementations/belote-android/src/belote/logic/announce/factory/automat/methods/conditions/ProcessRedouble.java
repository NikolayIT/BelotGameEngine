/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.methods.conditions.base.AnnounceCondition;

/**
 * ProcessRedouble class.
 * @author Dimitar Karamanov
 */
public final class ProcessRedouble implements AnnounceCondition {

    /**
     * BelotGame instance.
     */
    private final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public ProcessRedouble(final Game game) {
        this.game = game;
    }

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        if (player.getTeam().getPoints().getAllPoints() + 2 * announce.getAnnounceSuit().getBasePoints() >= Game.END_GAME_POINTS) {
            return false;
        }
        return true;
    }
}
