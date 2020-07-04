/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.methods.NullPlayMethod;
import belote.logic.play.strategy.validators.NullValidator;

/**
 * NullPlayCardStrategy class.
 * @author Dimitar Karamanov
 */
public class NullPlayCardStrategy extends BasePlayStrategy {

    /**
     * Constructor.
     * @param game belote game instance.
     */
    public NullPlayCardStrategy(final Game game) {
        super(game, new NullValidator(game), new NullPlayMethod(game));
    }

    /**
     * Returns next attack player.
     * @param attack the trick attack card.
     * @return next attack player.
     */
    protected Player getNextAttackPlayer(final Card attack) {
        return null;
    }
}
