IP_VM_LOCAL="172.16.117.128"
scp -r "C#-driver" samuel@$IP_VM_LOCAL:~/fromHost/
ssh samuel@$IP_VM_LOCAL  "cd ~/fromHost/C#-driver/ && ./build.sh"

scp -r samuel@$IP_VM_LOCAL:"~/fromHost/C#-driver/tinynf-sam/tinynf-sam/bin/Debug/netcoreapp3.1/linux-x64/*" tinynf-sam/code/
ssh samuel@$IP_VM_LOCAL  "rm -rf ~/fromHost/C#-driver/"

#send to DSLab machine
scp -r "tinynf-sam" samuelchassot@icnalsp3s3.epfl.ch:~/receiveBox/

#run it on server
# ssh samuelchassot@icnalsp3s3.epfl.ch "cd ~/receiveBox/C#-driver/tinynf-sam/benchmarking && ./bench.sh ../code latency 3"

# echo "waiting 30sec for execution"


# scp  samuelchassot@icnalsp3s3.epfl.ch:~/receiveBox/C#-driver/tinynf-sam/benchmarking/bench.log .