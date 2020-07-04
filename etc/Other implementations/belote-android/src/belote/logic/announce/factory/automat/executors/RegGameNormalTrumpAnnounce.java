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
import belote.logic.announce.factory.automat.methods.RegGameNormalFiftyOrHundredAnnounce;
import belote.logic.announce.factory.automat.methods.RegGameNormalTerzaAnnounce;

/**
 * RegGameNormalTrumpAnnounce class.
 * @author Dimitar Karamanov
 */
public final class RegGameNormalTrumpAnnounce extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public RegGameNormalTrumpAnnounce(final Game game) {
        super(game);

        register(new RegGameNormalFiftyOrHundredAnnounce(game));
        register(new RegGameNormalTerzaAnnounce(game));
        register(new RegGameNormalSimpleAnnounce(game));
    }
}