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
import belote.logic.announce.factory.automat.methods.EndGameNormalAllTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.EndGameNormalNoTrumpAnnounce;

/**
 * EndGameNormalNormalAnnounce class.
 * @author Dimitar Karamanov
 */
public final class EndGameNormalNormalAnnounce extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameNormalNormalAnnounce(final Game game) {
        super(game);

        register(new EndGameNormalAllTrumpAnnounce(game));
        register(new EndGameNormalNoTrumpAnnounce(game));
        register(new EndGameNormalTrumpAnnounce(game));
    }
}
