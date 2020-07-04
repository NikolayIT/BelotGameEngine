/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.adviser;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.ProcessRedouble;

/**
 * BaseRedoubleAdviser class.
 * @author Dimitar Karamanov
 */
public abstract class BaseRedoubleAdviser extends ConditionListMethod {

    /**
     * Constructor.
     * @param game Belote game instance.
     */
    public BaseRedoubleAdviser(final Game game) {
        super(game);
        addPreCondition(new ProcessRedouble(game));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected final Announce createAnnounce(final Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        return Announce.createRedoubleAnnounce(announce, player);
    }
}
