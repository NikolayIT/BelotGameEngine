/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.executors.trumpsLess;

import belote.bean.Game;
import belote.logic.play.strategy.automat.base.executor.PlayCardExecutor;
import belote.logic.play.strategy.automat.methods.MaxSingleNoHandCardToPartner;
import belote.logic.play.strategy.automat.methods.MinimumOfAllCard;
import belote.logic.play.strategy.automat.methods.MinMeterSuitCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.ChooseTwoSuitsCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.ClearCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.MaximumOfAllMastersCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.MinimumOfAllMastersCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.SingleNoMajorCard;
import belote.logic.play.strategy.automat.methods.trumpsLess.SingleNoMaxCard;

/**
 * NeedlessCard class. Implements the logic to play no needed card and is called after obligatory rules. Used in AllTrumptXXX and NoTrumpXXX
 * executors.
 * @author Dimitar Karamanov
 */
public final class NeedlessCard extends PlayCardExecutor {

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public NeedlessCard(final Game game) {
        super(game);
        // Register play card methods.
        register(new MaxSingleNoHandCardToPartner(game));
        register(new MaximumOfAllMastersCard(game));
        register(new MinimumOfAllMastersCard(game));
        register(new SingleNoMajorCard(game));
        register(new ClearCard(game));
        register(new SingleNoMaxCard(game));
        register(new MinMeterSuitCard(game));
        register(new ChooseTwoSuitsCard(game));
        register(new MinimumOfAllCard(game));
    }
}
