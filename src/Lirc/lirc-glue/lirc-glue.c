#include <stdlib.h>
#include <stdio.h>
#include <lirc/lirc_client.h>

int errorval = 0;
struct lirc_config *config;

void lirc_glue_set_error (int err)
{
    errorval = err;
}

int lirc_glue_get_error ()
{
    return (errorval);
}

int lirc_glue_readconfig ()
{
    int ret;

    if ((ret = lirc_readconfig(NULL, &config, NULL)) != 0)
        fprintf(stderr, "lirc-sharp-glue: error running lirc_readconfig");

    return (ret);
}
    
char *lirc_glue_next_valid_command ()
{
    if (config == NULL) {
        // FIXME: sleep 1s - just in case we're in a tight while loop
        lirc_glue_set_error (-1); // can't read the config!
        return (NULL);
    }
    char *code;
    char *command;
    
    while (lirc_nextcode (&code) == 0)
    {
        if (code == NULL) {
            lirc_glue_set_error (2); // means we got a null code...this is strange, but nothing to worry about
        }
        
        if (lirc_code2char (config, code, &command) == 0 && command != NULL) {
            lirc_glue_set_error (0); // means we have a code
        } else {
            lirc_glue_set_error (1);  // means we don't have a code.  totally normal for other unrelated button presses
        }

        free (code);

        if (lirc_glue_get_error () < 1 ) {
            return (command);
        }
    }

    return (NULL); // daemon shut down so no next code
}

struct lirc_config *lirc_glue_getconfig ()
{
    return (config);
}

void lirc_glue_freeconfig ()
{
    lirc_freeconfig (config);
}

/* lirc_client direct wrappers */
int lirc_glue_init (char *prog, int verbose)
{ return lirc_init (prog, verbose); }

int lirc_glue_deinit (void)
{ return lirc_deinit (); }

int lirc_glue_nextcode (char **code)
{ return lirc_nextcode (code); }
