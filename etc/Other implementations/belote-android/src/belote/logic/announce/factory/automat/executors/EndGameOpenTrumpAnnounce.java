/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.executors;

import belote.bean.Game;
import belote.logic.announce.factory.automat.executors.base.AnnounceExecutor;
import belote.logic.announce.factory.automat.methods.EndGameOpenFiftyOrHundredAnnounce;
import belote.logic.announce.factory.automat.methods.EndGameOpenTerzaAnnounce;

/**
 * EndGameOpenTrumpAnnounce class.
 * @author Dimitar Karamanov
 */
public final class EndGameOpenTrumpAnnounce extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameOpenTrumpAnnounce(final Game game) {
        super(game);

        register(new EndGameOpenFiftyOrHundredAnnounce(game));
        register(new EndGameOpenTerzaAnnounce(game));
        register(new EndGameOpenSimpleAnnounce(game));
    }
}